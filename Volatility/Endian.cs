public enum Endian
{
    Agnostic = -1,
    LE = 0,
    BE = 1
}

public static class EndianMapping
{
    private static readonly Dictionary<string, Endian> mapping = new(StringComparer.OrdinalIgnoreCase)
    {
        { "PS3", Endian.BE },
        { "X360", Endian.BE },
        { "TUB", Endian.LE },
        { "BPR", Endian.LE }
    };

    public static Endian GetDefaultEndian(string key)
    {
        if (mapping.TryGetValue(key, out Endian endian))
            return endian;

        return Endian.Agnostic;
    }

    public static Endian GetDefaultEndian(Volatility.Resources.Platform platform)
    {
        return GetDefaultEndian(platform.ToString());
    }
}