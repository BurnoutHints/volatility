using Volatility.Utilities;

public class EndianAwareBinaryWriter : BinaryWriter
{
    private Endian _endianness;

    public EndianAwareBinaryWriter(Stream output, Endian endianness) : base(output)
    {
        if (endianness == Endian.Agnostic)
            throw new InvalidOperationException("An agnostic endianness was passed to EndianAwareBinaryWriter! Ensure that the operation passes a valid endianness before attempting to use the writer.");

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
    public override void Write(ushort value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(short value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(uint value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(int value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(ulong value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(long value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(float value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public override void Write(double value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value) : value);
    }

    public void Write(Vector2 value, bool intrinsic = false)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.X) : value.X);
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Y) : value.Y);
        if (intrinsic) base.Write(new byte[0x8]);
    }

    public void Write(Vector3 value, bool intrinsic = false)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.X) : value.X);
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Y) : value.Y);
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Z) : value.Z);
        if (intrinsic) base.Write(new byte[0x4]);
    }

    public void Write(Vector4 value)
    {
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.X) : value.X);
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Y) : value.Y);
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Z) : value.Z);
        base.Write(_endianness == Endian.BE ? EndianUtilities.SwapEndian(value.W) : value.W);
    }
}
