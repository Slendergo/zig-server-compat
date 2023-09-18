using System.Text;

namespace wServer.realm;

public class DbEventArgs : EventArgs {
    public DbEventArgs(string message) {
        Message = message;
    }

    public string Message { get; private set; }
}

public class DbEvents {
    public DbEvents(RealmManager manager) {
        var db = manager.Database;

        // setup event for expiring keys
        db.Sub.Subscribe($"__keyevent@{db.DatabaseIndex}__:expired",
            (s, buff) => { Expired?.Invoke(this, new DbEventArgs(Encoding.UTF8.GetString(buff))); });
    }

    public event EventHandler<DbEventArgs> Expired;
}