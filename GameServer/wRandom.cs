namespace GameServer;

public sealed class WRandom 
{
    private uint _seed = 834569746;

    public WRandom() { }
    public WRandom(uint seed)
    {
        _seed = seed;
    }

    public uint NextInt() {
        return Gen();
    }

    public double NextDouble() {
        return Gen() / 2147483647.0;
    }

    public double NextNormal(double min = 0, double max = 1) {
        var j = Gen() / 2147483647;
        var k = Gen() / 2147483647;
        var l = Math.Sqrt(-2 * Math.Log(j)) * Math.Cos(2 * k * Math.PI);
        return min + l * max;
    }

    public uint NextIntRange(uint min, uint max) {
        var ret = min == max ? min : min + Gen() % (max - min);
        //Log.Info($"NextIntRange: {ret}");
        return ret;
    }

    public double NextDoubleRange(double min, double max) {
        return min + (max - min) * NextDouble();
    }

    private uint Gen() {
        var lb = 16807 * (_seed & 0xFFFF);
        var hb = 16807 * (uint) ((int) _seed >> 16);
        
        lb += ((hb & 32767) << 16);
        lb += (uint) ((int) hb >> 15);
        
        if (lb > 2147483647) 
            lb -= 2147483647;

        //Console.WriteLine("Seed: " + lb);
        return _seed = lb;
    }
}