using Volatility.Utilities;

public class EndianAwareBinaryReader : BinaryReader
{
    public Endian Endianness { get; protected set; }

    public EndianAwareBinaryReader(Stream input, Endian endianness) : base(input)
    {
        if (endianness == Endian.Agnostic)
            throw new InvalidOperationException("An agnostic endianness was passed to EndianAwareBinaryReader! Ensure that the operation passes a valid endianness before attempting to use the reader.");

        SetEndianness(endianness);
    }

    public void SetEndianness(Endian endianness)
    {
        Endianness = endianness;
    }

    public override ushort ReadUInt16()
    {
        ushort value = base.ReadUInt16();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override short ReadInt16()
    {
        short value = base.ReadInt16();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override uint ReadUInt32()
    {
        uint value = base.ReadUInt32();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override int ReadInt32()
    {
        int value = base.ReadInt32();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override ulong ReadUInt64()
    {
        ulong value = base.ReadUInt64();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override long ReadInt64()
    {
        long value = base.ReadInt64();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override float ReadSingle()
    {
        float value = base.ReadSingle();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }

    public override double ReadDouble()
    {
        double value = base.ReadDouble();
        return Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value;
    }
}
