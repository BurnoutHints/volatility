namespace Volatility.Resource.Splicer;

public class SplicerBE(string path) : SplicerBase(path)
{
    protected override Endian GetResourceEndian() => Endian.BE;
}