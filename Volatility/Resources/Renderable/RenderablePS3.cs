namespace Volatility.Resources;

public class RenderablePS3 : RenderableBase
{
    public override Endian ResourceEndian => Endian.BE;
    public override Platform ResourcePlatform => Platform.PS3;

    public RenderablePS3(string path, Endian endianness = Endian.Agnostic) : base(path, endianness) { }
}
