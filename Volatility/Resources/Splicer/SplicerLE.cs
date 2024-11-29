namespace Volatility.Resources.Splicer;

public class SplicerLE(string path) : SplicerBase(path)
{
    public override Endian GetResourceEndian() => Endian.LE;
}