using NLog;
using System.Collections.Concurrent;
using wServer.networking;
using wServer.networking.packets;

namespace wServer.realm;

class Work
{
    public Client client;
    public C2SPacketId id;
    public byte[] packet;

    public Work(Client client, C2SPacketId packetId, byte[] packet)
    {
        this.client = client;
        this.id = packetId;
        this.packet = packet;
    }
}

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
        Pendings.Add(new Work(client, id, packet));
    }

    public void TickLoop()
    {
        Log.Info("Network loop started.");
        foreach (var pending in Pendings.GetConsumingEnumerable())
        { // this foreach loop never exits. It blocks when work is not available.
            if (_manager.Terminating)
                break;

            if (pending.client.State == ProtocolState.Disconnected)
                continue;
            
            if (pending.client.Reconnecting)
            {
                Console.WriteLine($"Ignoring packet handler during reconnect.");
                continue;
            }

            try
            {
                Console.WriteLine("Handling: " + pending.id);

                var packet = Packet.C2SPackets[pending.id].CreateInstance();
                packet.Read(pending.client, pending.packet, 0, pending.packet.Length);
                pending.client.ProcessPacket(packet);
            }
            catch (Exception e)
            {
                Log.Error("Error processing packet ({0}, {1}, {2})\n{3}",
                    (pending.client.Account != null) ? pending.client.Account.Name : "<No account>",
                    pending.client.IP, pending.client, e);

                pending.client.SendFailure("An error occurred while processing data from your client.");
            }
        }
        Log.Info("Network loop stopped.");
    }

    public void Shutdown()
    {
        if (_manager.Terminating != true)
            throw new Exception("Must terminate realm manager before shutting down network ticker.");

        Pendings.Add(new Work(null, 0, null)); // dummy to allow loop to execute
    }
}