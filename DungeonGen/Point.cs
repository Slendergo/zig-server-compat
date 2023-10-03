namespace DungeonGen;

// Token: 0x02000006 RID: 6
public struct Point {
    // Token: 0x0600000F RID: 15 RVA: 0x00002288 File Offset: 0x00000488
    public Point(int x, int y) {
        X = x;
        Y = y;
    }

    // Token: 0x06000010 RID: 16 RVA: 0x00002298 File Offset: 0x00000498
    public Point(double x, double y) {
        this = new Point((int) Math.Round(x), (int) Math.Round(y));
    }

    // Token: 0x06000011 RID: 17 RVA: 0x000022AE File Offset: 0x000004AE
    public override string ToString() {
        return string.Format("({0}, {1})", X, Y);
    }

    // Token: 0x04000007 RID: 7
    public static readonly Point Zero = default;

    // Token: 0x04000008 RID: 8
    public readonly int X;

    // Token: 0x04000009 RID: 9
    public readonly int Y;
}