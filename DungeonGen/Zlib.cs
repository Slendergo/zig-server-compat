using System.IO.Compression;

namespace DungeonGen;

// Token: 0x0200000B RID: 11
public static class Zlib {
    // Token: 0x0600002E RID: 46 RVA: 0x0000267C File Offset: 0x0000087C
    private static uint ADLER32(byte[] data) {
        var num = 1u;
        var num2 = 0u;
        for (var i = 0; i < data.Length; i++) {
            num = (num + data[i]) % 65521u;
            num2 = (num2 + num) % 65521u;
        }

        return num2 << 16 | num;
    }

    // Token: 0x0600002F RID: 47 RVA: 0x000026B8 File Offset: 0x000008B8
    public static byte[] Decompress(byte[] buffer) {
        if (buffer.Length < 6) throw new ArgumentException("Invalid ZLIB buffer.");
        var b = buffer[0];
        var b2 = buffer[1];
        var b3 = (byte) (b & 15);
        var b4 = (byte) (b >> 4);
        if (b3 != 8) throw new NotSupportedException("Invalid compression method.");
        if (b4 != 7) throw new NotSupportedException("Unsupported window size.");
        var flag = (b2 & 32) != 0;
        if (flag) throw new NotSupportedException("Preset dictionary not supported.");
        if (((b << 8) + b2) % 31 != 0) throw new InvalidDataException("Invalid header checksum");
        var stream = new MemoryStream(buffer, 2, buffer.Length - 6);
        var memoryStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(stream, CompressionMode.Decompress)) {
            deflateStream.CopyTo(memoryStream);
        }

        var array = memoryStream.ToArray();
        var num = buffer.Length - 4;
        var num2 = (uint) (buffer[num++] << 24 | buffer[num++] << 16 | buffer[num++] << 8 | buffer[num++]);
        if (num2 != ADLER32(array)) throw new InvalidDataException("Invalid data checksum");
        return array;
    }

    public static byte[] Compress(byte[] buffer) {
        byte[] comp;
        using (var output = new MemoryStream()) {
            using (var deflate = new DeflateStream(output, CompressionMode.Compress)) {
                deflate.Write(buffer, 0, buffer.Length);
            }

            comp = output.ToArray();
        }

        // Refer to http://www.ietf.org/rfc/rfc1950.txt for zlib format
        const byte CM = 8;
        const byte CINFO = 7;
        const byte CMF = CM | CINFO << 4;
        const byte FLG = 0xDA;

        var result = new byte[comp.Length + 6];
        result[0] = CMF;
        result[1] = FLG;
        Buffer.BlockCopy(comp, 0, result, 2, comp.Length);

        var cksum = ADLER32(buffer);
        var index = result.Length - 4;
        result[index++] = (byte) (cksum >> 24);
        result[index++] = (byte) (cksum >> 16);
        result[index++] = (byte) (cksum >> 8);
        result[index++] = (byte) (cksum >> 0);

        return result;
    }
}