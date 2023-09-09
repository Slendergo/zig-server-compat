using System.Text.RegularExpressions;
using wServer.networking.packets.outgoing;

namespace wServer.realm.entities;

partial class Player
{
    static Regex nonAlphaNum = new("[^a-zA-Z0-9 ]", RegexOptions.CultureInvariant);
    static Regex repetition = new("(.)(?<=\\1\\1)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private int LastMessageDeviation = Int32.MaxValue;
    private string LastMessage = "";
    private long LastMessageTime = 0;
    private bool Spam = false;

    public bool CompareAndCheckSpam(string message, long time)
    {
        if (time - LastMessageTime < 500)
        {
            LastMessageTime = time;
            if (Spam)
            {
                return true;
            }
            else
            {
                Spam = true;
                return false;
            }
        }

        string strippedMessage = nonAlphaNum.Replace(message, "").ToLower();
        strippedMessage = repetition.Replace(strippedMessage, "");

        if (time - LastMessageTime > 10000)
        {
            LastMessageDeviation = LevenshteinDistance(LastMessage, strippedMessage);
            LastMessageTime = time;
            LastMessage = strippedMessage;
            Spam = false;
            return false;
        }
        else
        {
            int deviation = LevenshteinDistance(LastMessage, strippedMessage);
            LastMessageTime = time;
            LastMessage = strippedMessage;

            if (LastMessageDeviation <= LengthThreshold(LastMessage.Length) && deviation <= LengthThreshold(message.Length))
            {
                LastMessageDeviation = deviation;
                if (Spam)
                {
                    return true;
                }
                else
                {
                    Spam = true;
                    return false;
                }
            }
            else
            {
                LastMessageDeviation = deviation;
                Spam = false;
                return false;
            }
        }
    }

    public static int LengthThreshold(int length)
    {
        return length > 4 ? 3 : 0;
    }

    public static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (int i = 0; i <= n; d[i, 0] = i++) ;

        for (int j = 0; j <= m; d[0, j] = j++) ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

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