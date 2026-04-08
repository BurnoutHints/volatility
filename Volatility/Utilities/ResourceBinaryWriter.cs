using Volatility.Resources;
using Volatility.Utilities;

public class ResourceBinaryWriter : EndianAwareBinaryWriter
{
    public ResourceBinaryWriter(Stream output, Endian endianness) : base(output, endianness) { }

    public void Write(Vector2 value, bool intrinsic = false)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.X) : value.X);
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Y) : value.Y);
        if (intrinsic) base.Write(new byte[0x8]);
    }

    public void Write(Vector3 value, bool intrinsic = false)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.X) : value.X);
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Y) : value.Y);
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Z) : value.Z);
        if (intrinsic) base.Write(new byte[0x4]);
    }

    public void Write(Vector4 value)
    {
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.X) : value.X);
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Y) : value.Y);
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.Z) : value.Z);
        base.Write(Endianness == Endian.BE ? EndianUtilities.SwapEndian(value.W) : value.W);
    }

    public void WritePointer(ulong value, Arch arch)
    {
        if (arch == Arch.x64)
        {
           Write(value);
            return;
        }

        if (value > uint.MaxValue)
        {
            throw new InvalidDataException($"Pointer value 0x{value:X} does not fit in a 32-bit resource!");
        }

        Write((uint)value);
    }

    public void WriteSection<T>(long offset, T data, Action<ResourceBinaryWriter, T> writeItem)
    {
        if (offset == 0)
        {
            return;
        }

        BaseStream.Position = offset;
        writeItem(this, data);
    }

    public void WriteSection<T>(long offset, T data, Action<ResourceBinaryWriter, T, int> writeItem)
    {
        if (offset == 0)
        {
            return;
        }

        BaseStream.Position = offset;
        writeItem(this, data, 0);
    }

    public void WriteSection<T>(long offset, List<T> data, Action<ResourceBinaryWriter, T> writeItem)
    {
        if (offset == 0 || data.Count == 0)
        {
            return;
        }

        BaseStream.Position = offset;
        for (int i = 0; i < data.Count; i++)
        {
            writeItem(this, data[i]);
        }
    }

    public void WriteSection<T>(long offset, List<T> data, Action<ResourceBinaryWriter, T, int> writeItem)
    {
        if (offset == 0 || data.Count == 0)
        {
            return;
        }

        BaseStream.Position = offset;
        for (int i = 0; i < data.Count; i++)
        {
            writeItem(this, data[i], i);
        }
    }

    public void WriteSection<T>(ulong offset, T data, Action<ResourceBinaryWriter, T> writeItem)
    {
        if (offset == 0)
        {
            return;
        }

        BaseStream.Position = (long)offset;
        writeItem(this, data);
    }

    public void WriteSection<T>(ulong offset, T data, Action<ResourceBinaryWriter, T, int> writeItem)
    {
        if (offset == 0)
        {
            return;
        }

        BaseStream.Position = (long)offset;
        writeItem(this, data, 0);
    }

    public void WriteSection<T>(ulong offset, List<T> data, Action<ResourceBinaryWriter, T> writeItem)
    {
        if (offset == 0 || data.Count == 0)
        {
            return;
        }

        BaseStream.Position = (long)offset;
        for (int i = 0; i < data.Count; i++)
        {
            writeItem(this, data[i]);
        }
    }

    public void WriteSection<T>(ulong offset, List<T> data, Action<ResourceBinaryWriter, T, int> writeItem)
    {
        if (offset == 0 || data.Count == 0)
        {
            return;
        }

        BaseStream.Position = (long)offset;
        for (int i = 0; i < data.Count; i++)
        {
            writeItem(this, data[i], i);
        }
    }

    public void WriteFixedBytes(byte[]? data, int count)
    {
        byte[] output = new byte[count];
        if (data != null)
        {
            Array.Copy(data, output, Math.Min(data.Length, count));
        }

        Write(output);
    }

    public void WriteArchDependInt(int count, Arch arch)
    {
        Write(count);
        if (arch == Arch.x64)
        {
            Write(0x00000000);
        }
    }

    public void WriteArchDependUInt(uint count, Arch arch)
    {
        Write(count);
        if (arch == Arch.x64)
        {
            Write(0x00000000);
        }
    }
}
