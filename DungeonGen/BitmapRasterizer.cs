namespace DungeonGen;

// Token: 0x0200001F RID: 31
public class BitmapRasterizer<TPixel> where TPixel : struct {
    // Token: 0x04000114 RID: 276
    private const int SEG_COUNT = 10;

    // Token: 0x04000115 RID: 277

    // Token: 0x04000118 RID: 280
    private readonly bool[][,] caps;

    // Token: 0x04000116 RID: 278

    // Token: 0x04000117 RID: 279

    // Token: 0x0600011E RID: 286 RVA: 0x000047BC File Offset: 0x000029BC
    public BitmapRasterizer(int width, int height) {
        var array = new bool[5][,];
        var array2 = array;
        var num = 0;
        var array3 = new bool[1, 1];
        array3[0, 0] = true;
        array2[num] = array3;
        array[1] = new[,] {
            {true, true},
            {true, true}
        };
        array[2] = new[,] {
            {false, true, false},
            {true, true, true},
            {false, true, false}
        };
        array[3] = new[,] {
            {false, true, true, false},
            {true, true, true, true},
            {true, true, true, true},
            {false, true, true, false}
        };
        array[4] = new[,] {
            {false, true, true, true, false},
            {true, true, true, true, true},
            {true, true, true, true, true},
            {true, true, true, true, true},
            {false, true, true, true, false}
        };
        caps = array;
        Bitmap = new TPixel[width, height];
        Width = width;
        Height = height;
    }

    // Token: 0x17000049 RID: 73
    // (get) Token: 0x0600011F RID: 287 RVA: 0x00004861 File Offset: 0x00002A61
    public TPixel[,] Bitmap { get; }

    // Token: 0x1700004A RID: 74
    // (get) Token: 0x06000120 RID: 288 RVA: 0x00004869 File Offset: 0x00002A69
    public int Width { get; }

    // Token: 0x1700004B RID: 75
    // (get) Token: 0x06000121 RID: 289 RVA: 0x00004871 File Offset: 0x00002A71
    public int Height { get; }

    // Token: 0x06000122 RID: 290 RVA: 0x0000487C File Offset: 0x00002A7C
    public void Clear(TPixel bg) {
        for (var i = 0; i < Height; i++)
        for (var j = 0; j < Width; j++)
            Bitmap[j, i] = bg;
    }

    // Token: 0x06000123 RID: 291 RVA: 0x000048BC File Offset: 0x00002ABC
    private void FillRectInternal(int minX, int minY, int maxX, int maxY, TPixel pix) {
        for (var i = minY; i < maxY; i++)
        for (var j = minX; j < maxX; j++)
            Bitmap[j, i] = pix;
    }

    // Token: 0x06000124 RID: 292 RVA: 0x000048F4 File Offset: 0x00002AF4
    private void FillRectInternal(int minX, int minY, int maxX, int maxY, Func<int, int, TPixel> texMapping) {
        for (var i = minY; i < maxY; i++)
        for (var j = minX; j < maxX; j++)
            Bitmap[j, i] = texMapping(j, i);
    }

    // Token: 0x06000125 RID: 293 RVA: 0x00004930 File Offset: 0x00002B30
    public void FillRect(int x, int y, int w, int h, TPixel pix) {
        FillRectInternal(x, y, x + w, y + h, pix);
    }

    // Token: 0x06000126 RID: 294 RVA: 0x00004943 File Offset: 0x00002B43
    public void FillRect(Rect rect, TPixel pix) {
        FillRectInternal(rect.X, rect.Y, rect.MaxX, rect.MaxY, pix);
    }

    // Token: 0x06000127 RID: 295 RVA: 0x00004968 File Offset: 0x00002B68
    private void ApplyCap(int x, int y, TPixel pix, int width) {
        if (width == 1) Bitmap[x, y] = pix;
        if (width <= 5) {
            var array = caps[width - 1];
            x -= width >> 1;
            y -= width >> 1;
            for (var i = 0; i < width; i++)
            for (var j = 0; j < width; j++)
                if (array[j, i])
                    Bitmap[x + j, y + i] = pix;
            return;
        }

        var num = width >> 1;
        x -= num;
        y -= num;
        FillRectInternal(x, y, x + width, y + width, pix);
    }

    // Token: 0x06000128 RID: 296 RVA: 0x00004A00 File Offset: 0x00002C00
    private void ApplyCap(int x, int y, Func<int, int, TPixel> texMapping, int width) {
        if (width == 1) Bitmap[x, y] = texMapping(x, y);
        if (width <= 5) {
            var array = caps[width - 1];
            x -= width >> 1;
            y -= width >> 1;
            for (var i = 0; i < width; i++)
            for (var j = 0; j < width; j++)
                if (array[j, i])
                    Bitmap[x + j, y + i] = texMapping(x + j, y + i);
            return;
        }

        var num = width >> 1;
        x -= num;
        y -= num;
        FillRectInternal(x, y, x + width, y + width, texMapping);
    }

    // Token: 0x06000129 RID: 297 RVA: 0x00004AAA File Offset: 0x00002CAA
    public void DrawLine(Point a, Point b, TPixel pix, int width = 1) {
        DrawLine(a.X, a.Y, b.X, b.Y, pix, width);
    }

    // Token: 0x0600012A RID: 298 RVA: 0x00004AD4 File Offset: 0x00002CD4
    public void DrawLine(int x0, int y0, int x1, int y1, TPixel pix, int width = 1) {
        var num = Math.Abs(x1 - x0);
        var num2 = Math.Abs(y1 - y0);
        var num3 = x0 < x1 ? 1 : -1;
        var num4 = y0 < y1 ? 1 : -1;
        var num5 = num - num2;
        while (x0 != x1 || y0 != y1) {
            ApplyCap(x0, y0, pix, width);
            var num6 = 2 * num5;
            if (num6 > -num2) {
                num5 -= num2;
                x0 += num3;
            }
            else {
                num5 += num;
                y0 += num4;
            }
        }

        ApplyCap(x0, y0, pix, width);
    }

    // Token: 0x0600012B RID: 299 RVA: 0x00004B53 File Offset: 0x00002D53
    public void DrawLine(Point a, Point b, Func<int, int, TPixel> texMapping, int width = 1) {
        DrawLine(a.X, a.Y, b.X, b.Y, texMapping, width);
    }

    // Token: 0x0600012C RID: 300 RVA: 0x00004B7C File Offset: 0x00002D7C
    public void DrawLine(int x0, int y0, int x1, int y1, Func<int, int, TPixel> texMapping, int width = 1) {
        var num = Math.Abs(x1 - x0);
        var num2 = Math.Abs(y1 - y0);
        var num3 = x0 < x1 ? 1 : -1;
        var num4 = y0 < y1 ? 1 : -1;
        var num5 = num - num2;
        ApplyCap(x0, y0, texMapping, width);
        while (x0 != x1 || y0 != y1) {
            var num6 = 2 * num5;
            if (num6 > -num2) {
                num5 -= num2;
                x0 += num3;
            }
            else {
                num5 += num;
                y0 += num4;
            }

            ApplyCap(x0, y0, texMapping, width);
        }
    }

    // Token: 0x0600012D RID: 301 RVA: 0x00004BFC File Offset: 0x00002DFC
    private void ScanEdge(int x0, int y0, int x1, int y1, int?[] min, int?[] max) {
        var num = Math.Abs(x1 - x0);
        var num2 = Math.Abs(y1 - y0);
        var num3 = x0 < x1 ? 1 : -1;
        var num4 = y0 < y1 ? 1 : -1;
        var num5 = num - num2;
        if (min[y0] == null || min[y0] > x0) min[y0] = x0;
        if (max[y0] == null || max[y0] < x0) max[y0] = x0;
        while (x0 != x1 || y0 != y1) {
            var num6 = 2 * num5;
            if (num6 >= -num2) {
                num5 -= num2;
                x0 += num3;
            }

            if (num6 <= num) {
                num5 += num;
                y0 += num4;
            }

            if (min[y0] == null || min[y0] > x0) min[y0] = x0;
            if (max[y0] == null || max[y0] < x0) max[y0] = x0;
        }
    }

    // Token: 0x0600012E RID: 302 RVA: 0x00004DA0 File Offset: 0x00002FA0
    public void FillTriangle(Point a, Point b, Point c, TPixel color) {
        var num = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        var num2 = Math.Max(a.Y, Math.Max(b.Y, c.Y)) + 1;
        var num3 = num2 - num;
        var array = new int?[num3];
        var array2 = new int?[num3];
        ScanEdge(a.X, a.Y - num, b.X, b.Y - num, array, array2);
        ScanEdge(b.X, b.Y - num, c.X, c.Y - num, array, array2);
        ScanEdge(c.X, c.Y - num, a.X, a.Y - num, array, array2);
        for (var i = num; i < num2; i++) {
            var value = array[i - num].Value;
            var value2 = array2[i - num].Value;
            for (var j = value; j <= value2; j++) Bitmap[j, i] = color;
        }
    }

    // Token: 0x0600012F RID: 303 RVA: 0x00004ED4 File Offset: 0x000030D4
    public void FillTriangle(Point a, Point b, Point c, Func<int, int, TPixel> texMapping) {
        var num = Math.Min(a.Y, Math.Min(b.Y, c.Y));
        var num2 = Math.Max(a.Y, Math.Max(b.Y, c.Y)) + 1;
        var num3 = num2 - num;
        var array = new int?[num3];
        var array2 = new int?[num3];
        ScanEdge(a.X, a.Y - num, b.X, b.Y - num, array, array2);
        ScanEdge(b.X, b.Y - num, c.X, c.Y - num, array, array2);
        ScanEdge(c.X, c.Y - num, a.X, a.Y - num, array, array2);
        for (var i = num; i < num2; i++) {
            var value = array[i - num].Value;
            var value2 = array2[i - num].Value;
            for (var j = value; j <= value2; j++) Bitmap[j, i] = texMapping(j, i);
        }
    }

    // Token: 0x06000130 RID: 304 RVA: 0x00005010 File Offset: 0x00003210
    public void DrawBezier(Point a, Point cp, Point b, TPixel pix, int width = 1) {
        DrawBezier(a.X, a.Y, cp.X, cp.Y, b.X, b.Y, pix, width);
    }

    // Token: 0x06000131 RID: 305 RVA: 0x00005054 File Offset: 0x00003254
    public void DrawBezier(int x0, int y0, int x1, int y1, int x2, int y2, TPixel pix, int width = 1) {
        var num = (double) x0;
        var num2 = (double) y0;
        for (var i = 0; i < 10; i++) {
            var num3 = (i + 1) / 10.0;
            var num4 = 1.0 - num3;
            var num5 = num4 * num4 * x0 + 2.0 * num4 * num3 * x1 + num3 * num3 * x2;
            var num6 = num4 * num4 * y0 + 2.0 * num4 * num3 * y1 + num3 * num3 * y2;
            DrawLine((int) num, (int) num2, (int) num5, (int) num6, pix, width);
            num = num5;
            num2 = num6;
        }
    }
}