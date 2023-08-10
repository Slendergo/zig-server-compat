using NLog;
using System.Collections.Concurrent;
using wServer.networking;
using wServer.networking.packets;

namespace wServer.realm;

using Work = Tuple<Client, int, C2SPacketId, byte[]>;

public class NetworkTicker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly RealmManager _manager;
    private static readonly BlockingCollection<Work> Pendings = new();

    public NetworkTicker(RealmManager manager)
    {
        _manager = manager;
    }

    public static void AddPendingPacket(Client client, C2SPacketId id, byte[] packet)
    {
        Pendings.Add(new Work(client, client.Id, id, packet));
    }

    public void TickLoop()
    {
        Log.Info("Network loop started.");
        foreach (var pending in Pendings.GetConsumingEnumerable())
        { // this foreach loop never exits. It blocks when work is not available.
            if (_manager.Terminating)
                break;

            if (pending.Item1.Id != pending.Item2 || pending.Item1.State == ProtocolState.Disconnected)
                continue;

            try
            {
                var packet = Packet.C2SPackets[pending.Item3].CreateInstance();
                packet.Read(pending.Item1, pending.Item4, 0, pending.Item4.Length);
                pending.Item1.ProcessPacket(packet);
            }
            catch (Exception e)
            {
                Log.Error("Error processing packet ({0}, {1}, {2})\n{3}",
                    (pending.Item1.Account != null) ? pending.Item1.Account.Name : "",
                    pending.Item1.IP, pending.Item2, e);

                pending.Item1.SendFailure("An error occurred while processing data from your client.");
            }
        }
        Log.Info("Network loop stopped.");
    }

    public void Shutdown()
    {
        if (_manager.Terminating != true)
            throw new Exception("Must terminate realm manager before shutting down network ticker.");

        Pendings.Add(new Work(null, 0, 0, null)); // dummy to allow loop to execute
    }
}