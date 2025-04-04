using Volatility.Utilities;

public class ResourceBinaryReader : EndianAwareBinaryReader
{
    public ResourceBinaryReader(Stream input, Endian endianness) : base(input, endianness) { }

    public Vector2 ReadVector2()
    {
        Vector2 value = new Vector2(base.ReadSingle(), base.ReadSingle());
        base.BaseStream.Seek(0x8, SeekOrigin.Current);
        return value;
    }

    public Vector2 ReadVector2Literal()
    {
        return new Vector2(base.ReadSingle(), base.ReadSingle());
    }
    
    public Vector3 ReadVector3()
    {
        Vector3 value = new Vector3(base.ReadSingle(), base.ReadSingle(), base.ReadSingle());
        base.BaseStream.Seek(0x4, SeekOrigin.Current);
        return value;
    }

    public ColorRGB ReadColorRGB()
    {
        return (ColorRGB)ReadVector3();
    }

    public Vector4 ReadVector4()
    {
        return new Vector4(base.ReadSingle(), base.ReadSingle(), base.ReadSingle(), base.ReadSingle());
    }

    public ColorRGBA ReadColorRGBA()
    {
        return (ColorRGBA)ReadVector4();
    }
}