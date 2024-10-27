namespace Volatility.Resource.Splicer;

public class SplicerBE(string path) : SplicerBase(path)
{
    public override Endian GetResourceEndian() => Endian.BE;
}