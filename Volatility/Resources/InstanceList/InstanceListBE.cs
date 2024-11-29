namespace Volatility.Resources.InstanceList;

public class InstanceListBE(string path) : InstanceListBase(path)
{
    public override Endian GetResourceEndian() => Endian.BE;
}
