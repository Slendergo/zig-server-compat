using System.Text;
using common;
using System.Net;
using wServer.networking.packets.incoming;
using NLog;

namespace wServer.networking.packets;

public abstract class Packet
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static readonly Dictionary<C2SPacketId, Packet> C2SPackets = new();

    public Client Owner { get; private set; }

    static Packet()
    {
        foreach (var i in typeof(Packet).Assembly.GetTypes())
            if (typeof(Packet).IsAssignableFrom(i) && !i.IsAbstract)
            {
                Packet pkt = (Packet)Activator.CreateInstance(i);
                if (pkt is IncomingMessage)
                    C2SPackets.Add(pkt.C2SId, pkt);
            }
    }

    public virtual C2SPacketId C2SId => C2SPacketId.Unknown;
    public virtual S2CPacketId S2CId => S2CPacketId.Unknown;
    public abstract Packet CreateInstance();

    public void SetOwner(Client client)
    {
        Owner = client;
    }
    
    public void Read(Client client, byte[] body, int offset, int len)
    {
        //Crypt(client, body, offset, len);
        Read(new NReader(new MemoryStream(body)));
    }

    public int Write(byte[] buff, int offset)
    {
        var s = new MemoryStream();
        Write(new NWriter(s));

        var len = (int)s.Position + 3;
        if (len > buff.Length - offset)
            return 0;
        
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)len - 2), 0, buff, offset, 2);
        buff[offset + 2] = (byte)S2CId;
        Buffer.BlockCopy(s.GetBuffer(), 0, buff, offset + 3, len - 3);
        return len;
    }

    protected abstract void Read(NReader rdr);
    protected abstract void Write(NWriter wtr);

    public override string ToString()
    {
        // buggy...
        var ret = new StringBuilder("{");
        var arr = GetType().GetProperties();
        for (var i = 0; i < arr.Length; i++)
        {
            if (i != 0) ret.Append(", ");
            ret.Append($"{arr[i].Name}: {arr[i].GetValue(this, null)}");
        }
        ret.Append('}');
        return ret.ToString();
    }
}