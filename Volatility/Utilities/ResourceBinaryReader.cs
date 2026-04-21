using Volatility.Resources;

public class ResourceBinaryReader : EndianAwareBinaryReader
{
    public ResourceBinaryReader(Stream input, Endian endianness) : base(input, endianness) { }

    public Vector2 ReadVector2()
    {
        Vector2 value = new(base.ReadSingle(), base.ReadSingle());
        base.BaseStream.Seek(0x8, SeekOrigin.Current);
        return value;
    }

    public Vector2 ReadVector2Literal()
    {
        return new Vector2(base.ReadSingle(), base.ReadSingle());
    }
    
    public Vector3 ReadVector3()
    {
        Vector3 value = new(base.ReadSingle(), base.ReadSingle(), base.ReadSingle());
        base.BaseStream.Seek(0x4, SeekOrigin.Current);
        return value;
    }

    public ColorRGB ReadColorRGB()
    {
        return (ColorRGB)ReadVector3();
    }

    public Vector3Plus ReadVector3Plus()
    {
        return ReadVector4();
    }

    public Vector4 ReadVector4()
    {
        return new Vector4(base.ReadSingle(), base.ReadSingle(), base.ReadSingle(), base.ReadSingle());
    }

    public ColorRGBA ReadColorRGBA()
    {
        return (ColorRGBA)ReadVector4();
    }

    public ColorRGBA8 ReadColorRGBA8()
    {
        return (ColorRGBA8)ReadUInt32();
    }

    internal ulong ReadArchValue(Arch arch)
    {
        return arch == Arch.x64 ? ReadUInt64() : ReadUInt32();
    }

    public ulong ReadPointer(Arch arch)
    {
        return ReadArchValue(arch);
    }

    public void ParseSection<T>(ulong offset, int count, Func<ResourceBinaryReader, T> parser, List<T> destination)
    {
        if (count <= 0 || offset == 0)
        {
            return;
        }

        long originalPosition = BaseStream.Position;
        BaseStream.Seek((long)offset, SeekOrigin.Begin);

        for (int i = 0; i < count; i++)
        {
            destination.Add(parser(this));
        }

        BaseStream.Seek(originalPosition, SeekOrigin.Begin);
    }

    public List<T> ParseSection<T>(ulong offset, int count, Func<ResourceBinaryReader, T> parser)
    {
        List<T> destination = new(Math.Max(count, 0));
        ParseSection(offset, count, parser, destination);
        return destination;
    }

    public void ParseSection<T>(long offset, int count, Func<ResourceBinaryReader, T> parser, List<T> destination)
    {
        if (count <= 0 || offset == 0)
        {
            return;
        }

        long originalPosition = BaseStream.Position;
        BaseStream.Seek(offset, SeekOrigin.Begin);

        for (int i = 0; i < count; i++)
        {
            destination.Add(parser(this));
        }

        BaseStream.Seek(originalPosition, SeekOrigin.Begin);
    }

    public List<T> ParseSection<T>(long offset, int count, Func<ResourceBinaryReader, T> parser)
    {
        List<T> destination = new(Math.Max(count, 0));
        ParseSection(offset, count, parser, destination);
        return destination;
    }

    public void ParseSection<T>(ulong offset, Func<ResourceBinaryReader, T> parser, out T destination)
    {
        if (offset == 0)
        {
            destination = default!;
            return;
        }

        long originalPosition = BaseStream.Position;
        BaseStream.Seek((long)offset, SeekOrigin.Begin);

        try
        {
            destination = parser(this);
        }
        finally
        {
            BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }

    public void ParseSection<T>(long offset, Func<ResourceBinaryReader, T> parser, out T destination)
    {
        if (offset == 0)
        {
            destination = default!;
            return;
        }

        long originalPosition = BaseStream.Position;
        BaseStream.Seek(offset, SeekOrigin.Begin);

        try
        {
            destination = parser(this);
        }
        finally
        {
            BaseStream.Seek(originalPosition, SeekOrigin.Begin);
        }
    }

    public int ReadArchDependInt(Arch arch)
    {
        int value = ReadInt32();
        if (arch == Arch.x64)
        {
            BaseStream.Seek(0x4, SeekOrigin.Current);
        }

        return value;
    }

    public uint ReadArchDependUInt(Arch arch)
    {
        uint value = ReadUInt32();
        if (arch == Arch.x64)
        {
            BaseStream.Seek(0x4, SeekOrigin.Current);
        }

        return value;
    }
}
