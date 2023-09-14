using System.Text.RegularExpressions;
using wServer.networking.packets.outgoing;

namespace wServer.realm.entities;

partial class Player
{
    public void SendInfo(string text) => _client.SendText(string.Empty, 0, -1, 0, string.Empty, text);
    public void SendInfoFormat(string text, params object[] args) => _client.SendText(string.Empty, 0, -1, 0, string.Empty, string.Format(text, args));

    public void SendErrorText(string text) => _client.SendErrorText(text);
    public void SendErrorFormat(string text, params object[] args) => _client.SendErrorText(string.Format(text, args));

    public void SendClientText(string text) => _client.SendText("*Client*", 0, -1, 0, string.Empty, text);
    public void SendClientTextFormat(string text, params object[] args) => _client.SendText("*Client*", 0, -1, 0, string.Empty, string.Format(text, args));

    public void SendHelp(string text) => _client.SendText("*Help*", 0, -1, 0, string.Empty, text);
    public void SendHelpFormat(string text, params object[] args) => _client.SendText("*Help*", 0, -1, 0, string.Empty, string.Format(text, args));

    public void SendEnemy(string name, string text) => _client.SendText($"#{name}", 0, -1, 0, string.Empty, text);
    public void SendEnemyFormat(string name, string text, params object[] args) => _client.SendText($"#{name}", 0, -1, 0, string.Empty, string.Format(text, args));

    public void TellReceived(int objId, int stars, string from, string to, string text) => _client.SendText(from, objId, stars, 10, to, text);
    public void AnnouncementReceived(string text) => _client.Player.SendInfo(string.Concat("<ANNOUNCEMENT> ", text));
    public void GuildReceived(int objId, int stars, string from, string text) => _client.SendText(from, objId, stars, 10, "*Guild*", text);
}