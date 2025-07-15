using Volatility.Utilities;

namespace Volatility.Extensions;

public static class ArchExtensions
{
    public static int PointerSize(this Arch arch) => arch == Arch.x64 ? 8 : 4;
}

public static class BinaryStreamExtensions
{
    public static ushort ReadUInt16(this BinaryReader r, Endian endian)
    {
        ushort v = r.ReadUInt16();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static short ReadInt16(this BinaryReader r, Endian endian)
    {
        short v = r.ReadInt16();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static uint ReadUInt32(this BinaryReader r, Endian endian)
    {
        uint v = r.ReadUInt32();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static int ReadInt32(this BinaryReader r, Endian endian)
    {
        int v = r.ReadInt32();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static ulong ReadUInt64(this BinaryReader r, Endian endian)
    {
        ulong v = r.ReadUInt64();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static long ReadInt64(this BinaryReader r, Endian endian)
    {
        long v = r.ReadInt64();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static float ReadSingle(this BinaryReader r, Endian endian)
    {
        float v = r.ReadSingle();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static double ReadDouble(this BinaryReader r, Endian endian)
    {
        double v = r.ReadDouble();
        return endian == Endian.BE ? EndianUtilities.SwapEndian(v) : v;
    }

    public static ulong ReadPointer(this BinaryReader r, Arch arch, Endian endian)
        => arch == Arch.x64
            ? r.ReadUInt64(endian)
            : r.ReadUInt32(endian);
    
    public static uint ReadUInt32Padded(this BinaryReader r, Arch arch, Endian endian)
    {
        uint val = r.ReadUInt32(endian);
        int pad = arch.PointerSize() - sizeof(uint);
        if (pad > 0) r.BaseStream.Seek(pad, SeekOrigin.Current);
        return val;
    }

    public static T ReadEnum<T>(this BinaryReader r, Endian endian) where T : Enum
    {
        var underlying = Enum.GetUnderlyingType(typeof(T));

        object raw = underlying switch
        {
            Type t when t == typeof(byte)  => r.ReadByte(),
            Type t when t == typeof(short) => r.ReadInt16(endian),
            Type t when t == typeof(ushort)=> r.ReadUInt16(endian),
            Type t when t == typeof(int)   => r.ReadInt32(endian),
            Type t when t == typeof(uint)  => r.ReadUInt32(endian),
            _ => throw new NotSupportedException($"Unsupported enum type {underlying}")
        };

        return (T)Enum.ToObject(typeof(T), raw);
    }
    
    public static void WritePointer(this BinaryWriter w, Arch arch, ulong ptr, Endian endian)
    {
        if (arch == Arch.x64) w.Write(ptr,    endian);
        else                  w.Write((uint)ptr, endian);
    }

    public static void WriteUInt32Padded(this BinaryWriter w, Arch arch, uint val, Endian endian)
    {
        w.Write(val, endian);
        int pad = arch.PointerSize() - sizeof(uint);
        if (pad > 0) w.Write(new byte[pad]);
    }

    public static void Write(this BinaryWriter w, ushort v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, short v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, uint v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, int v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, ulong v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, long v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, float v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }

    public static void Write(this BinaryWriter w, double v, Endian endian)
    {
        if (endian == Endian.BE) v = EndianUtilities.SwapEndian(v);
        w.Write(v);
    }
    
    public static void WriteEnum<T>(this BinaryWriter w, T value, Endian endian)
        where T : Enum
    {
        var underlying = Enum.GetUnderlyingType(typeof(T));
        Action write = underlying switch
        {
            Type t when t == typeof(byte)   => () => w.Write(Convert.ToByte(value)),
            Type t when t == typeof(sbyte)  => () => w.Write(Convert.ToSByte(value)),
            Type t when t == typeof(short)  => () => w.Write(Convert.ToInt16(value), endian),
            Type t when t == typeof(ushort) => () => w.Write(Convert.ToUInt16(value), endian),
            Type t when t == typeof(int)    => () => w.Write(Convert.ToInt32(value), endian),
            Type t when t == typeof(uint)   => () => w.Write(Convert.ToUInt32(value), endian),
            Type t when t == typeof(long)   => () => w.Write(Convert.ToInt64(value), endian),
            Type t when t == typeof(ulong)  => () => w.Write(Convert.ToUInt64(value), endian),
            _ => throw new NotSupportedException(
                $"Enum underlying type '{underlying.FullName}' is not supported")
        };
        write();
    }
}