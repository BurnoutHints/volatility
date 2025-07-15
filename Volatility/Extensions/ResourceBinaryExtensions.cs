namespace Volatility.Extensions;

public static class ResourceBinaryExtensions
{
    public static Vector2 ReadVector2(this BinaryReader r, Endian endian)
    {
        float x = r.ReadSingle(endian);
        float y = r.ReadSingle(endian);
        r.BaseStream.Seek(0x8, SeekOrigin.Current);
        return new Vector2(x, y);
    }

    public static Vector2 ReadVector2Literal(this BinaryReader r, Endian endian)
        => new Vector2(r.ReadSingle(endian), r.ReadSingle(endian));

    public static Vector3 ReadVector3(this BinaryReader r, Endian endian)
    {
        float x = r.ReadSingle(endian);
        float y = r.ReadSingle(endian);
        float z = r.ReadSingle(endian);
        r.BaseStream.Seek(0x4, SeekOrigin.Current);
        return new Vector3(x, y, z);
    }

    public static ColorRGB ReadColorRGB(this BinaryReader r, Endian endian)
        => (ColorRGB)r.ReadVector3(endian);

    public static Vector3Plus ReadVector3Plus(this BinaryReader r, Endian endian)
        => new Vector4(
            r.ReadSingle(endian),
            r.ReadSingle(endian),
            r.ReadSingle(endian),
            r.ReadSingle(endian)
        );
    
    public static Vector4 ReadVector4(this BinaryReader r, Endian endian)
        => new Vector4(
            r.ReadSingle(endian),
            r.ReadSingle(endian),
            r.ReadSingle(endian),
            r.ReadSingle(endian)
        );

    public static ColorRGBA ReadColorRGBA(this BinaryReader r, Endian endian)
        => (ColorRGBA)r.ReadVector4(endian);
    
    public static void Write(this BinaryWriter w, Vector2 value, Endian endian, bool intrinsic = false)
    {
        w.Write(value.X, endian);
        w.Write(value.Y, endian);
        if (intrinsic)
            w.Write(new byte[0x8]);
    }

    public static void Write(this BinaryWriter w, Vector3 value, Endian endian, bool intrinsic = false)
    {
        w.Write(value.X, endian);
        w.Write(value.Y, endian);
        w.Write(value.Z, endian);
        if (intrinsic)
            w.Write(new byte[0x4]);
    }

    public static void Write(this BinaryWriter w, Vector4 value, Endian endian)
    {
        w.Write(value.X, endian);
        w.Write(value.Y, endian);
        w.Write(value.Z, endian);
        w.Write(value.W, endian);
    }
}