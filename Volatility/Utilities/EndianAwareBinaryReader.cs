using Volatility.Utilities;

public class EndianAwareBinaryReader : BinaryReader
{
    private Endian _endianness;

    public EndianAwareBinaryReader(Stream input, Endian endianness) : base(input)
    {
        SetEndianness(endianness);
    }

    public void SetEndianness(Endian endianness)
    {
        _endianness = endianness;
    }

    public Endian GetEndianness()
    {
        return _endianness;
    }

    public override ushort ReadUInt16()
    {
        ushort value = base.ReadUInt16();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override short ReadInt16()
    {
        short value = base.ReadInt16();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override uint ReadUInt32()
    {
        uint value = base.ReadUInt32();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override int ReadInt32()
    {
        int value = base.ReadInt32();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override ulong ReadUInt64()
    {
        ulong value = base.ReadUInt64();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override long ReadInt64()
    {
        long value = base.ReadInt64();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override float ReadSingle()
    {
        float value = base.ReadSingle();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override double ReadDouble()
    {
        double value = base.ReadDouble();
        return _endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }
}
