using System;
using System.IO;
using System.IO.Compression;

namespace DungeonGenerator
{
    // Token: 0x0200000B RID: 11
    public static class Zlib
    {
        // Token: 0x0600002E RID: 46 RVA: 0x0000267C File Offset: 0x0000087C
        private static uint ADLER32(byte[] data)
        {
            uint num = 1u;
            uint num2 = 0u;
            for (int i = 0; i < data.Length; i++)
            {
                num = (num + (uint)data[i]) % 65521u;
                num2 = (num2 + num) % 65521u;
            }
            return num2 << 16 | num;
        }

        // Token: 0x0600002F RID: 47 RVA: 0x000026B8 File Offset: 0x000008B8
        public static byte[] Decompress(byte[] buffer)
        {
            if (buffer.Length < 6)
            {
                throw new ArgumentException("Invalid ZLIB buffer.");
            }
            byte b = buffer[0];
            byte b2 = buffer[1];
            byte b3 = (byte)(b & 15);
            byte b4 = (byte)(b >> 4);
            if (b3 != 8)
            {
                throw new NotSupportedException("Invalid compression method.");
            }
            if (b4 != 7)
            {
                throw new NotSupportedException("Unsupported window size.");
            }
            bool flag = (b2 & 32) != 0;
            if (flag)
            {
                throw new NotSupportedException("Preset dictionary not supported.");
            }
            if ((((int)b << 8) + (int)b2) % 31 != 0)
            {
                throw new InvalidDataException("Invalid header checksum");
            }
            MemoryStream stream = new MemoryStream(buffer, 2, buffer.Length - 6);
            MemoryStream memoryStream = new MemoryStream();
            using (DeflateStream deflateStream = new DeflateStream(stream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(memoryStream);
            }
            byte[] array = memoryStream.ToArray();
            int num = buffer.Length - 4;
            uint num2 = (uint)((int)buffer[num++] << 24 | (int)buffer[num++] << 16 | (int)buffer[num++] << 8 | (int)buffer[num++]);
            if (num2 != Zlib.ADLER32(array))
            {
                throw new InvalidDataException("Invalid data checksum");
            }
            return array;
        }

		public static byte[] Compress(byte[] buffer) {
			byte[] comp;
			using (var output = new MemoryStream()) {
				using (var deflate = new DeflateStream(output, CompressionMode.Compress))
					deflate.Write(buffer, 0, buffer.Length);
				comp = output.ToArray();
			}

			// Refer to http://www.ietf.org/rfc/rfc1950.txt for zlib format
			const byte CM = 8;
			const byte CINFO = 7;
			const byte CMF = CM | (CINFO << 4);
			const byte FLG = 0xDA;

			byte[] result = new byte[comp.Length + 6];
			result[0] = CMF;
			result[1] = FLG;
			Buffer.BlockCopy(comp, 0, result, 2, comp.Length);

			uint cksum = ADLER32(buffer);
			var index = result.Length - 4;
			result[index++] = (byte)(cksum >> 24);
			result[index++] = (byte)(cksum >> 16);
			result[index++] = (byte)(cksum >> 8);
			result[index++] = (byte)(cksum >> 0);

			return result;
		}
    }
}