namespace wServer.realm.entities;

partial class Player {
    public void SendInfo(string text) {
        Client.SendText(string.Empty, 0, -1, 0, string.Empty, text);
    }

    public void SendInfoFormat(string text, params object[] args) {
        Client.SendText(string.Empty, 0, -1, 0, string.Empty, string.Format(text, args));
    }

    public void SendErrorText(string text) {
        Client.SendErrorText(text);
    }

    public void SendErrorFormat(string text, params object[] args) {
        Client.SendErrorText(string.Format(text, args));
    }

    public void SendClientText(string text) {
        Client.SendText("*Client*", 0, -1, 0, string.Empty, text);
    }

    public void SendClientTextFormat(string text, params object[] args) {
        Client.SendText("*Client*", 0, -1, 0, string.Empty, string.Format(text, args));
    }

    public void SendHelp(string text) {
        Client.SendText("*Help*", 0, -1, 0, string.Empty, text);
    }

    public void SendHelpFormat(string text, params object[] args) {
        Client.SendText("*Help*", 0, -1, 0, string.Empty, string.Format(text, args));
    }

    public void SendEnemy(string name, string text) {
        Client.SendText($"#{name}", 0, -1, 0, string.Empty, text);
    }

    public void SendEnemyFormat(string name, string text, params object[] args) {
        Client.SendText($"#{name}", 0, -1, 0, string.Empty, string.Format(text, args));
    }

    public void TellReceived(int objId, int stars, string from, string to, string text) {
        Client.SendText(from, objId, stars, 10, to, text);
    }

    public void AnnouncementReceived(string text) {
        Client.Player.SendInfo(string.Concat("<ANNOUNCEMENT> ", text));
    }

    public void GuildReceived(int objId, int stars, string from, string text) {
        Client.SendText(from, objId, stars, 10, "*Guild*", text);
    }
}