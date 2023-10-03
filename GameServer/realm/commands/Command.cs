using GameServer.realm.entities;
using GameServer.realm.entities.player;
using NLog;

namespace GameServer.realm.commands;

public abstract class Command {
    protected static readonly Logger Log = LogManager.GetCurrentClassLogger();

    protected Command(string name, bool reqAdmin = false, string alias = null, bool listCommand = true) {
        CommandName = name;
        RequiresAdmin = reqAdmin;
        ListCommand = listCommand;
        Alias = alias;
    }

    public string CommandName { get; }
    public string Alias { get; }
    public bool RequiresAdmin { get; }
    public bool ListCommand { get; private set; }

    protected abstract bool Process(Player player, RealmTime time, string args);

    public bool HasPermission(Player player) {
        return !RequiresAdmin || RequiresAdmin && player.Client.Account.Admin;
    }

    public bool Execute(Player player, RealmTime time, string args, bool bypassPermission = false) {
        if (!bypassPermission && !HasPermission(player)) {
            player.SendErrorText("No permission!");
            return false;
        }

        try {
            return Process(player, time, args);
        }
        catch (Exception ex) {
            Log.Error("Error when executing the command.", ex);
            player.SendErrorText("Error when executing the command.");
            return false;
        }
    }
}

public class CommandManager {
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, Command> _cmds;

    private RealmManager _manager;

    public CommandManager(RealmManager manager) {
        _manager = manager;
        _cmds = new Dictionary<string, Command>(StringComparer.InvariantCultureIgnoreCase);
        var t = typeof(Command);
        foreach (var i in t.Assembly.GetTypes())
            if (t.IsAssignableFrom(i) && i != t) {
                var instance = i.GetConstructor(new[] {typeof(RealmManager)}) == null
                    ? (Command) Activator.CreateInstance(i)
                    : (Command) Activator.CreateInstance(i, manager);
                _cmds.Add(instance.CommandName, instance);
                if (instance.Alias != null) _cmds.Add(instance.Alias, instance);
            }
    }

    public IDictionary<string, Command> Commands => _cmds;

    public bool Execute(Player player, RealmTime time, string text) {
        var index = text.IndexOf(' ');
        var cmd = text.Substring(1, index == -1 ? text.Length - 1 : index - 1);
        var args = index == -1 ? "" : text.Substring(index + 1);

        if (!_cmds.TryGetValue(cmd, out var command)) {
            player.SendErrorText("Unknown command!");
            return false;
        }

        var owner = player.Owner;
        Log.Info("[Command] [{0}] <{1}> {2}", owner?.IdName ?? "", player.Name, text);
        return command.Execute(player, time, args);
    }
}