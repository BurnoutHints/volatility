using Volatility.Utilities;

public class EndianAwareBinaryWriter : BinaryWriter
{
    public Endian Endianness { get; protected set; }

    public EndianAwareBinaryWriter(Stream output, Endian endianness) : base(output)
    {
        if (endianness == Endian.Agnostic)
            throw new InvalidOperationException("An agnostic endianness was passed to EndianAwareBinaryWriter! Ensure that the operation passes a valid endianness before attempting to use the writer.");

        SetEndianness(endianness);
    }

    public void SetEndianness(Endian endianness)
    {
        Endianness = endianness;
    }

    public override void Write(ushort value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(short value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(uint value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(int value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(ulong value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(long value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(float value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(double value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }
}
