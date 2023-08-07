using System;
using System.Linq;
using common;
using wServer.networking.packets.outgoing;
using wServer.realm.entities;
using wServer.realm.worlds;

namespace wServer.realm
{
    public class DbServerManager
    {
        private struct Message
        {
            public string Type;
            public string Inst;

            public string TargetPlayer;
        }

        private readonly RealmManager manager;

        public DbServerManager(RealmManager manager)
        {
            this.manager = manager;
            manager.InterServer.AddHandler<Message>(Channel.Control, HandleControl);
        }

        private void HandleControl(object sender, InterServerEventArgs<Message> e)
        {
            switch (e.Content.Type)
            {
            }
        }
    }
}
