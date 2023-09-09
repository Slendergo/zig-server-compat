using common;
using NLog;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using wServer.networking.packets;
using wServer.networking.packets.incoming;
using wServer.networking.packets.outgoing;
using wServer.networking.server;
using wServer.realm;
using wServer.realm.entities;
using wServer.realm.worlds.logic;

using static wServer.networking.PacketUtils;

namespace wServer.networking;

public enum ProtocolState
{
    Disconnected,
    Connected,
    Handshaked,
    Ready
}

public partial class Client
{
    static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public RealmManager Manager { get; }
    
    private readonly Server _server;
    private readonly CommHandler _handler;


    private volatile ProtocolState _state;
    public ProtocolState State
    {
        get => _state;
        internal set => _state = value;
    }

    public int Id { get; internal set; }
    public DbAccount Account { get; internal set; }
    public DbChar Character { get; internal set; }
    public Player Player { get; internal set; }

    public wRandom Random { get; internal set; }
    public uint Seed { get; set; }

    //Temporary connection state
    internal int TargetWorld = -1;

    public Socket Socket { get; private set; }
    public string IP { get; private set; }

    internal readonly object DcLock = new();

    private readonly Memory<byte> ReceiveMemory;
    private readonly Memory<byte> SendMem;

    // dont think we need but its there incasse
    private object ReceiveLock = new object();

    private object SendLock = new object();

    public Client(Server server, RealmManager manager,
        SocketAsyncEventArgs send, SocketAsyncEventArgs receive)
    {
        _server = server;
        Manager = manager;

        _handler = new CommHandler(this, send, receive);

        ReceiveMemory = GC.AllocateArray<byte>(RECV_BUFFER_LEN, pinned: true).AsMemory();
        SendMem = GC.AllocateArray<byte>(SEND_BUFFER_LEN, pinned: true).AsMemory();
    }

    #region Send Methods

    public void SendFailure(int errorCode, string description)
    {
        var ptr = LENGTH_PREFIX;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.Failure);

        WriteInt(ref ptr, ref spanRef, errorCode);
        WriteString(ref ptr, ref spanRef, description);

        TrySend(ptr);
    }

    public void SendMapInfo(
        int width,
        int height,
        string idName,
        string displayName,
        uint seed,
        int difficulty,
        int background,
        bool allowTeleport,
        bool showDisplays,
        int bgLightColor,
        float bgLightIntensity,
        float dayLightIntensity,
        float nightLightIntensity,
        long totalElapsedMicroSeconds)
    {
        lock (SendLock)
        {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.MapInfo);

            WriteInt(ref ptr, ref spanRef, width);
            WriteInt(ref ptr, ref spanRef, height);
            WriteString(ref ptr, ref spanRef, idName);
            WriteString(ref ptr, ref spanRef, displayName);

            WriteUInt(ref ptr, ref spanRef, seed);
            WriteInt(ref ptr, ref spanRef, difficulty);
            WriteInt(ref ptr, ref spanRef, background);

            WriteBool(ref ptr, ref spanRef, allowTeleport);
            WriteBool(ref ptr, ref spanRef, showDisplays);

            WriteInt(ref ptr, ref spanRef, bgLightColor);
            WriteFloat(ref ptr, ref spanRef, bgLightIntensity);
            WriteBool(ref ptr, ref spanRef, dayLightIntensity != 0.0);
            if (dayLightIntensity != 0.0)
            {
                WriteFloat(ref ptr, ref spanRef, dayLightIntensity);
                WriteFloat(ref ptr, ref spanRef, nightLightIntensity);
                WriteLong(ref ptr, ref spanRef, totalElapsedMicroSeconds);
            }

            TrySend(ptr);
        }
    }

    public void SendAccountList(int accountListId, int[] accountIds)
    {
        lock (SendLock)
        {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.AccountList);

            WriteInt(ref ptr, ref spanRef, accountListId);

            WriteUShort(ref ptr, ref spanRef, (ushort)accountIds.Length);
            for (var i = 0; i < accountIds.Length; i++)
                WriteInt(ref ptr, ref spanRef, accountIds[i]);

            TrySend(ptr);
        }
    }

    public void SendCreateSuccess(int objectId, int charId)
    {
        lock (SendLock)
        {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.CreateSuccess);

            WriteInt(ref ptr, ref spanRef, objectId);
            WriteInt(ref ptr, ref spanRef, charId);

            TrySend(ptr);
        }
    }

    // kinda hacky but needed for client error with no player 
    public void SendErrorText(string text) => SendText("*Error*", 0, -1, 0, string.Empty, text);

    public void SendText(string name, int objectId, int numStars, byte bubbleTime, string recipient, string text)
    {
        lock (SendLock)
        {
            var ptr = LENGTH_PREFIX;
            ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
            WriteByte(ref ptr, ref spanRef, (byte)S2CPacketId.Text);

            WriteString(ref ptr, ref spanRef, name);
            WriteInt(ref ptr, ref spanRef, objectId);
            WriteInt(ref ptr, ref spanRef, numStars);
            WriteByte(ref ptr, ref spanRef, bubbleTime);
            WriteString(ref ptr, ref spanRef, recipient);
            WriteString(ref ptr, ref spanRef, text);

            TrySend(ptr);
        }
    }

    private async void TrySend(int len)
    {
        if (!Socket.Connected)
            return;

        try
        {
            // Log.Error($"Sending packet {(S2CPacketId) SendMem.Span[0]} {len}");
            BinaryPrimitives.WriteUInt16LittleEndian(SendMem.Span, (ushort)(len - LENGTH_PREFIX));
            _ = await Socket.SendAsync(SendMem[..len]);
        }
        catch (Exception e)
        {
            Disconnect();
            if (e is not SocketException se || (se.SocketErrorCode != SocketError.ConnectionReset && se.SocketErrorCode != SocketError.Shutdown))
                Log.Error($"{Account?.Name ?? "[unconnected]"} ({IP}): {e}");
        }
    }

    #endregion

    public void Reset()
    {
        Id = 0; // needed so that inbound packets that are currently queued are discarded.
        Account = null;
        Character = null;
        Player = null;

        // reset client ping/pong values
        _pingTime = -1;
        _pongTime = -1;

        _handler.Reset();
    }

    public void BeginHandling(Socket skt)
    {
        Socket = skt;

        try
        {
            IP = ((IPEndPoint)skt.RemoteEndPoint).Address.ToString();
        }
        catch (Exception e)
        {
            IP = "";
        }

        Log.Trace("Received client @ {0}.", IP);
        _handler.BeginHandling(Socket);
    }

    public void SendPacket(Packet pkt)
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State != ProtocolState.Disconnected)
                _handler.SendPacket(pkt);
        }
    }

    public void SendPackets(IEnumerable<Packet> pkts)
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State != ProtocolState.Disconnected)
                _handler.SendPackets(pkts);
        }
    }

    public bool IsReady()
    {
        switch (State)
        {
            case ProtocolState.Disconnected:
            case ProtocolState.Ready when Player?.Owner == null:
                return false;
            default:
                return true;
        }
    }

    internal void ProcessPacket(Packet pkt)
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State == ProtocolState.Disconnected)
                return;

            try
            {
                Log.Trace("Handling packet '{0}'...", pkt.C2SId);

                if (!PacketHandlers.C2SHandlers.TryGetValue(pkt.C2SId, out var handler))
                    Log.Warn("Unhandled packet '{0}'.", pkt.C2SId);
                else
                    handler.Handle(this, (IncomingMessage)pkt);
            }
            catch (Exception e)
            {
                Log.Error("Error when handling packet '{0}, {1}'...", pkt.ToString(), e.ToString());
                Disconnect("Packet handling error.");
            }
        }
    }

    public bool Reconnecting;

    public void Reconnect(string name, int gameId)
    {
        Reconnecting = true;

        if (Account == null)
        {
            Disconnect("Tried to reconnect an client with a null account...");
            return;
        }

        Log.Trace("Reconnecting client ({0}) @ {1} to {2}...", Account.Name, IP, name);
        ConnectManager.Reconnect(this, gameId);
    }

    public void Disconnect(string reason = "")
    {
        using (TimedLock.Lock(DcLock))
        {
            if (State == ProtocolState.Disconnected)
                return;

            SendFailure(0, reason);

            State = ProtocolState.Disconnected;

            if (!string.IsNullOrEmpty(reason))
                Log.Warn("Disconnecting client ({0}) @ {1}... {2}",
                    Account?.Name ?? " ", IP, reason);

            if (Account != null)
                try
                {
                    Save();
                }
                catch (Exception e)
                {
                    var msg = $"{e.Message}\n{e.StackTrace}";
                    Log.Error(msg);
                }

            Manager.Disconnect(this);
            _server.Disconnect(this);
        }
    }

    private void Save() // only when disconnect
    {
        var acc = Account;

        if (Character == null || Player == null || Player.Owner is Test)
        {
            Manager.Database.ReleaseLock(acc);
            return;
        }

        Player.SaveToCharacter();
        Player.Owner?.LeaveWorld(Player);
        acc.RefreshLastSeen();
        acc.FlushAsync();
        Manager.Database.SaveCharacter(acc, Character, Player.FameCounter.ClassStats, true)
            .ContinueWith(t => Manager.Database.ReleaseLock(acc));
    }

    public void Dispose()
    {
        // nothing to do here
    }
}