namespace Volatility.Resources.Renderable;

public class RenderableX360 : RenderableBase
{
    public override Endian GetResourceEndian() => Endian.BE;

    public RenderableX360(string path) : base(path) { }
}
