using System.Text;
using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.worlds.logic;

namespace GameServer.logic.behaviors;

internal class AnnounceOnDeath : Behavior {
    public static readonly string PLAYER_COUNT = "{COUNT}";
    public static readonly string PLAYER_LIST = "{PL_LIST}";
    private readonly string _message;

    public AnnounceOnDeath(XElement e) {
        _message = e.ParseString("@message");
    }

    public AnnounceOnDeath(string msg) {
        _message = msg;
    }

    protected internal override void Resolve(State parent) {
        parent.Death += (sender, e) => {
            if (e.Host.Spawned || e.Host.Owner is Test) return;

            var owner = e.Host.Owner;
            var players = owner.Players.Values
                .Where(p => p.Client != null)
                .ToArray();

            var sb = new StringBuilder();
            for (var i = 0; i < players.Length; i++) {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(players[i].Name);
            }

            var playerList = sb.ToString();
            var playerCount = owner.Players.Values.Count(p => p.Client != null).ToString();

            var announcement = _message.Replace(PLAYER_COUNT, playerCount).Replace(PLAYER_LIST, playerList);

            e.Host.Manager.Chat.Announce(announcement);
        };
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) { }
}