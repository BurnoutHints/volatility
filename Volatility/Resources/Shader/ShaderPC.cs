using System.Text;

namespace Volatility.Resources;

public class ShaderPC : ShaderBase
{
    public override Endian GetResourceEndian() => Endian.LE;
    public override Platform GetResourcePlatform() => Platform.TUB;

    public override void WriteToStream(EndianAwareBinaryWriter writer, Endian endianness)
    {
        base.WriteToStream(writer, endianness);
    }

    public override void ParseFromStream(ResourceBinaryReader reader, Endian endianness)
    {
        base.ParseFromStream(reader, endianness);

        long baseOffset = reader.BaseStream.Position;
        long returnOffset = reader.BaseStream.Position;

        long pointerOffset = baseOffset + 0x24;
        if (pointerOffset + sizeof(uint) <= reader.BaseStream.Length)
        {
            reader.BaseStream.Seek(pointerOffset, SeekOrigin.Begin);
            uint shaderSourcePtr = reader.ReadUInt32();
            ShaderSourceText = ReadNullTerminatedString(reader, baseOffset, shaderSourcePtr);
        }

        reader.BaseStream.Seek(returnOffset, SeekOrigin.Begin);
    }

    private static string? ReadNullTerminatedString(ResourceBinaryReader reader, long baseOffset, uint pointer)
    {
        if (pointer == 0)
            return null;

        long start = baseOffset + pointer;
        if (start < 0 || start >= reader.BaseStream.Length)
            return null;

        reader.BaseStream.Seek(start, SeekOrigin.Begin);

        long remaining = reader.BaseStream.Length - start;
        if (remaining <= 0)
            return string.Empty;

        int maxLength = remaining > int.MaxValue ? int.MaxValue : (int)remaining;
        byte[] buffer = reader.ReadBytes(maxLength);
        int terminator = Array.IndexOf(buffer, (byte)0);
        int length = terminator >= 0 ? terminator : buffer.Length;
        return length == 0 ? string.Empty : Encoding.UTF8.GetString(buffer, 0, length);
    }

    public ShaderPC() : base() { }

    public ShaderPC(string path) : base(path) { }
}
