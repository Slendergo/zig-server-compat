using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using common;
using common.resources;
using NLog;
using NLog.Targets;
using StackExchange.Redis;
using ServerType = common.ServerType;

namespace server; 

public class Program
{
	private static Logger Log = LogManager.GetCurrentClassLogger();
	internal static ServerConfig Config;
	internal static Resources Resources;
	internal static Database Database;
	internal static ISManager ISManager;
	private static Hashtable GetFiles;
	private static string ResourcePath;
        
	private static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
		Thread.CurrentThread.Name = "Entry";
		Config = args.Length != 0 ? ServerConfig.ReadFile(args[0] + "/server.json") : ServerConfig.ReadFile("server.json");
		var logConfig = new NLog.Config.LoggingConfiguration();
		var consoleTarget = new ColoredConsoleTarget("consoleTarget")
		{
			Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
		};
		logConfig.AddTarget(consoleTarget);
		logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
            
		var fileTarget = new FileTarget("fileTarget")
		{
			FileName = "${var:logDirectory}/log.txt",
			Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}"
		};
		logConfig.AddTarget(fileTarget);
		LogManager.Configuration = logConfig;
		LogManager.Configuration.Variables["logDirectory"] = (args.Length != 0 ? args[0] + "/" : "") + Config.serverSettings.logFolder + "/login";
		LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfiguration();

		ResourcePath = args.Length != 0 ? args[0] + "/resources" : Config.serverSettings.resourceFolder;
		Resources = new Resources(ResourcePath);
		using var database = Database = new Database(
			       Config.dbInfo.host,
			       Config.dbInfo.port,
			       Config.dbInfo.index,
			       Config.dbInfo.auth,
			       Resources);
		/*GetFiles = new Hashtable();
				foreach (KeyValuePair<string, byte[]> f in Resources.WebFiles)
				{
					Hashtable getFiles = GetFiles;
					string key = f.Key;
					GetFile getFile = new GetFile
					{
						Data = f.Value
					};
					string extension = Path.GetExtension(f.Key);
					getFile.ContentType = GetMimeType(extension.Substring(1, extension.Length - 1));
					getFiles.Add(key, getFile);
				}*/
		Config.serverInfo.instanceId = Guid.NewGuid().ToString();
		ISManager = new ISManager(Database, Config);
		ISManager.Run();
		var port = Config.serverInfo.port;
		var address = Config.serverInfo.bindAddress;

		using var server = new HttpListener();

		// stupid admin prompting bai
#if DEBUG
        server.Prefixes.Add($"http://*:{port}/");
#else
        server.Prefixes.Add($"http://{address}:{port}/");
#endif
        server.Start();

		Log.Info("Listening at address {0}:{1}...", address, port);
		while (true) {
			var ctx = server.GetContext();
			var req = ctx.Request;
			var resp = ctx.Response;
			if (req.HttpMethod == "GET")
				continue;
        					
			resp.StatusCode = 200;
			resp.ContentType = "text/plain";
			NameValueCollection query;
			using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
				query = HttpUtility.ParseQueryString(reader.ReadToEnd());
        					
			var result = req.RawUrl switch {
				"/account/changePassword" => HandleAccountChangePassword(query["email"], query["password"], query["newPassword"]), 
				"/account/purchaseCharSlot" => HandleAccountPurchaseCharSlot(query["email"], query["password"]), 
				"/account/purchaseSkin" => HandleAccountPurchaseSkin(query["email"], query["password"], query["skinType"]), 
				"/account/purchaseVaultSkin" => HandleAccountPurchaseVaultSkin(query["email"], query["password"], query["type"]), 
				"/account/register" => HandleAccountRegister(query["email"], query["password"], query["username"]), 
				"/account/verify" => HandleAccountVerify(query["email"], query["password"]), 
				"/app/init" => HandleAppInit(),
				"/char/list" => HandleCharList(query["email"], query["password"]), 
				"/char/delete" => HandleCharDelete(query["email"], query["password"], query["charId"]), 
				"/guild/getBoard" => HandleGetBoard(query["email"], query["password"]), 
				"/guild/listMembers" => HandleListMembers(query["email"], query["password"]), 
				"/guild/setBoard" => HandleSetBoard(query["email"], query["password"], query["board"]), 
				_ => HandleUnknown(req.RawUrl, req.RemoteEndPoint?.Address.ToString())
			};
				
			using var writer = new StreamWriter(resp.OutputStream, req.ContentEncoding);
			writer.Write(result);
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleUnknown(string endpoint, string ip) {
		Log.Error("Invalid endpoint " + endpoint + ", IP: " + ip);
		return "<Error>Invalid endpoint</Error>";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAccountChangePassword(string guid, string password, string newPassword) {
		var status = Database.Verify(guid, password, out var val);
		if (status != LoginStatus.OK)
			return "<Error>" + status.GetInfo() + "</Error>";
        		
		Database.ChangePassword(guid, newPassword);
		return "<Success />";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAccountPurchaseCharSlot(string guid, string password) {
		var status = Database.Verify(guid, password, out var acc);
		if (status != LoginStatus.OK)
			return "<Error>" + status.GetInfo() + "</Error>";
        		
		using var i = Database.Lock(acc);
		if (!Database.LockOk(i))
			return "<Error>Account in use</Error>";
        		
		/*var currency = (CurrencyType)0;
		if ((int)currency == 0 && acc.Fame < 1000)
			return "<Error>Insufficient funds</Error>";*/
        		
		var trans = Database.Conn.CreateTransaction();
		trans.AddCondition(Condition.HashEqual(acc.Key, "maxCharSlot", acc.MaxCharSlot));
		trans.HashIncrementAsync(acc.Key, "maxCharSlot");
		var t2 = trans.ExecuteAsync();
		t2.ContinueWith(_ => {
			if (t2.IsCanceled || !t2.Result)
				return "<Error>Internal Server Error</Error>";

			acc.MaxCharSlot++;
			return "<Success />";
		}).ContinueWith(e => { 
				Log.Error(e.Exception.InnerException.ToString());
				return "<Error>" + status.GetInfo() + "</Error>";
		}, TaskContinuationOptions.OnlyOnFaulted);
		
		return "<Success />";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAccountPurchaseSkin(string guid, string password, string skinType)
	{
		var status = Database.Verify(guid, password, out var acc);
		if (status != LoginStatus.OK)
			return "<Error>" + status.GetInfo() + "</Error>";
		
		var parsedType = (ushort)Utils.GetInt(skinType);
		var skinDesc = Resources.GameData.Skins[parsedType];
		if (skinDesc.Cost > acc.Fame)
			return "<Error>Failed to purchase skin</Error>";
		
		Database.PurchaseSkin(acc, parsedType, skinDesc.Cost);
		return "<Success />";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAccountPurchaseVaultSkin(string guid, string password, string skinType)
	{
		/*var status = Database.Verify(guid, password, out var acc);
		if ((int)status > 0)
		    return "<Error>" + status.GetInfo() + "</Error>";
		
		var objType = ushort.Parse(skinType);
		var containerDesc = Resources.GameData.ObjectDescs[objType];
		if (containerDesc.Cost > 0 && containerDesc.Cost > acc.Gems)
		    return "<Error>Failed to purchase vault skin</Error>";
		
		Database.PurchaseSkin(acc, objType, containerDesc.Cost, true);
		return "<Success/>";*/
		return "";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAccountRegister(string email, string password, string username) {
		if (!Utils.IsValidEmail(email))
			return "<Error>Invalid email</Error>";
        		
		string lockToken = null;
		try {
			while ((lockToken = Database.AcquireLock("regLock")) == null) { }
                    
			var s = Database.Register(email, password, username, out _);
			return s == RegisterStatus.OK ? "<Success />" : "<Error>" + s.GetInfo() + "</Error>";
		} finally {
			if (lockToken != null) {
				Database.ReleaseLock("regLock", lockToken);
			}
		}
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAccountVerify(string guid, string password) {
		var status = Database.Verify(guid, password, out var acc);
		if (status == LoginStatus.OK)
			return Account.FromDb(acc).ToXml().ToString();
        		
		return "<Error>" + status.GetInfo() + "</Error>";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleAppInit() {
		/*var root = new XElement(Resources.Settings.Xml);
		root.Add(new XElement("SkinsList", XElement.Parse(File.ReadAllText(Resources.ResourcePath + "/xml/Skins.xml"))));
		return root.ToString();*/
		return "";
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleCharList(string guid, string password) {
		var status = Database.Verify(guid, password, out var acc);
		if (status == LoginStatus.InvalidCredentials)
			return "<Error>Invalid account</Error>";
		
		if (status != LoginStatus.OK)
			return "<Error>" + status.GetInfo() + "</Error>";

		var list = CharList.FromDb(Database, acc);
		list.Servers = new List<ServerItem>();
		var serverList = ISManager.GetServerList();
		foreach (var server in serverList)
			if (server.type == ServerType.World)
				list.Servers.Add(new ServerItem {
					Name = server.name,
					Lat = server.coordinates.latitude,
					Long = server.coordinates.longitude,
					Port = server.port,
					DNS = server.address,
					Usage = (int) (server.players / (float)server.maxPlayers),
					AdminOnly = server.adminOnly
				});
                    
                
		return list.ToXml().ToString();
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleCharDelete(string guid, string password, string charId) {
		var status = Database.Verify(guid, password, out var acc);
		if (status != LoginStatus.OK)
			return "<Error>" + status.GetInfo() + "</Error>";
        		
		using var i = Database.Lock(acc);
		if (!Database.LockOk(i))
			return "<Error>Account in Use</Error>";
        		
		Database.DeleteCharacter(acc, int.Parse(charId));
		return "<Success />";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleGetBoard(string guid, string password) {
		var status = Database.Verify(guid, password, out var acc);
		if (status != LoginStatus.OK)
			return acc.GuildId <= 0 ? "<Error>Not in guild</Error>" : Database.GetGuild(acc.GuildId).Board;
        		
		return "<Error>" + status.GetInfo() + "</Error>";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleListMembers(string guid, string password) {
		var status = Database.Verify(guid, password, out var acc);
		if (status == LoginStatus.OK)
			return acc.GuildId <= 0 ? "<Error>Not in guild</Error>" : 
				Guild.FromDb(Database, Database.GetGuild(acc.GuildId)).ToXml().ToString();
        		
		return "<Error>" + status.GetInfo() + "</Error>";
	}
        
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static string HandleSetBoard(string guid, string password, string board) {
		var status = Database.Verify(guid, password, out var acc);
		if (status != LoginStatus.OK)
			return "<Error>" + status.GetInfo() + "</Error>";
        		
		if (acc.GuildId <= 0 || acc.GuildRank < 20)
			return "<Error>No permission</Error>";
        		
		var guild = Database.GetGuild(acc.GuildId);
		var text = HttpUtility.UrlDecode(board);
		return Database.SetGuildBoard(guild, text) ? text : "<Error>Failed to set board</Error>";
	}
        
	private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
	{
		Log.Fatal((Exception)args.ExceptionObject);
	}
}