namespace Volatility.Resources;

public class RenderablePS3 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;
    public override Platform GetResourcePlatform() => Platform.PS3;

    public RenderablePS3(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
