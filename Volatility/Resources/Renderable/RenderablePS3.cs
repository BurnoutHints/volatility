namespace Volatility.Resources.Renderable;

public class RenderablePS3 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;
    public override Platform GetResourcePlatform() => Platform.PS3;

    public RenderablePS3(string path) : base(path) { }
}
