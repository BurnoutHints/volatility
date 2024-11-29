namespace Volatility.Resources.InstanceList;

public class InstanceListLE(string path) : InstanceListBase(path)
{
    public override Endian GetResourceEndian() => Endian.LE;
}
