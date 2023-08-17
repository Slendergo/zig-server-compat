using common;

namespace wServer.networking.packets.outgoing;

public class MapInfo : OutgoingMessage
{
    public int Width { get; set; }
    public int Height { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public int Difficulty { get; set; }
    public uint Seed { get; set; }
    public int Background { get; set; }
    public bool AllowPlayerTeleport { get; set; }
    public bool ShowDisplays { get; set; }
    public int BackgroundLightColor { get; set; }
    public float BackgroundLightIntensity { get; set; }
    public float DayLightIntensity { get; set; }
    public float NightLightIntensity { get; set; }
    
    public int GameTime { get; set; }

    public override S2CPacketId S2CId => S2CPacketId.MapInfo;
    public override Packet CreateInstance() { return new MapInfo(); }

    protected override void Read(NReader rdr)
    {
    }

    protected override void Write(NWriter wtr)
    {
        wtr.Write(Width);
        wtr.Write(Height);
        wtr.WriteUTF(Name);
        wtr.WriteUTF(DisplayName);
        wtr.Write(Seed);
        wtr.Write(Background);
        wtr.Write(Difficulty);
        wtr.Write(AllowPlayerTeleport);
        wtr.Write(ShowDisplays);
        
        wtr.Write(BackgroundLightColor);
        wtr.Write(BackgroundLightIntensity);
        
        wtr.Write(DayLightIntensity != 0);
        
        if (DayLightIntensity != 0)
        {
            wtr.Write(DayLightIntensity);
            wtr.Write(NightLightIntensity);
            wtr.Write(GameTime);
        }
    }
}