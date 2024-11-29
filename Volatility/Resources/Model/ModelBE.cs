namespace Volatility.Resources.Model;

public class ModelBE(string path) : ModelBase(path)
{
    public override Endian GetResourceEndian() => Endian.BE;
}
