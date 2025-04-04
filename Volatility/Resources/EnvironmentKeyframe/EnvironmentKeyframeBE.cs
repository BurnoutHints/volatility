namespace Volatility.Resources.EnvironmentKeyframe;

public class EnvironmentKeyframeBE(string path) : EnvironmentKeyframeBase(path)
{
    public override Endian GetResourceEndian() => Endian.BE;
}
