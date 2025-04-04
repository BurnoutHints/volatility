namespace Volatility.Resources.EnvironmentKeyframe;

public class EnvironmentKeyframeLE(string path) : EnvironmentKeyframeBase(path)
{
    public override Endian GetResourceEndian() => Endian.LE;
}
