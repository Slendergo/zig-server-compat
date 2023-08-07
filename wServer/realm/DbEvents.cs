using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wServer.realm
{
    public class DbEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public DbEventArgs(string message)
        {
            Message = message;
        }
    }

    public class DbEvents
    {
        public event EventHandler<DbEventArgs> Expired;

        public DbEvents(RealmManager manager)
        {
            var db = manager.Database;

            // setup event for expiring keys
            db.Sub.Subscribe($"__keyevent@{db.DatabaseIndex}__:expired", (s, buff) =>
            {
                Expired?.Invoke(this, new DbEventArgs(Encoding.UTF8.GetString(buff)));
            });
        }
    }
}
