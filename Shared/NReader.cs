using System.Text;

namespace Shared;

public class NReader : BinaryReader {
    public NReader(Stream s) : base(s, Encoding.UTF8) { }

    public string ReadUTF() {
        return Encoding.UTF8.GetString(ReadBytes(ReadUInt16()));
    }

    public string Read32UTF() {
        return Encoding.UTF8.GetString(ReadBytes(ReadInt32()));
    }
}