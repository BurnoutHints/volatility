﻿using Volatility.Utilities;

public class EndianAwareBinaryWriter : BinaryWriter
{
    private Endian _endianness;

    public EndianAwareBinaryWriter(Stream output, Endian endianness) : base(output)
    {
        SetEndianness(endianness);
    }

    public void SetEndianness(Endian endianness)
    {
        _endianness = endianness;
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
}
