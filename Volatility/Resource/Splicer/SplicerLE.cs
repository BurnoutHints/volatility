namespace Volatility.Resource.Splicer;

public class SplicerLE(string path) : SplicerBase(path)
{
    protected override Endian GetResourceEndian() => Endian.LE;
}