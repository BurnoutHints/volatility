namespace Volatility.Resources;

public class RenderableX360 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;
    public override Platform GetResourcePlatform() => Platform.X360;

    public RenderableX360(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
