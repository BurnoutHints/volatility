namespace Volatility.Resources.Renderable;

public class RenderablePS3 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;

    public RenderablePS3(string path) : base(path) { }
}
