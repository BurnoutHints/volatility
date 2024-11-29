namespace Volatility.Resources.Model;

public class ModelLE(string path) : ModelBase(path)
{
    public override Endian GetResourceEndian() => Endian.LE;
}
