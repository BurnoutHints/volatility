
global using ResourceID = StrongID<ResourceTag>;
global using SnrID = StrongID<SnrTag>;

using System.Text;

public interface IStrongID
{
    ulong Value { get; }
}
public interface IIDTag
{
    static abstract ulong Hash(string text);
    static abstract ulong Hash(ReadOnlySpan<byte> data);
}

public readonly record struct StrongID<TTag>(ulong Value) where TTag : IIDTag
{
    public static StrongID<TTag> Default => new(0UL);
    public static implicit operator ulong(StrongID<TTag> id) => id.Value;
    public static implicit operator StrongID<TTag>(ulong v) => new(v);
    public static implicit operator StrongID<TTag>(string s) => HashFromString(s);
    public static StrongID<TTag> HashFromString(string s) => new(TTag.Hash(s));         // For creating IDs from names
    public static StrongID<TTag> HashFromBytes(byte[] b) => new(TTag.Hash(b));          // For creating IDs from data (SnrID)
    public static StrongID<TTag> FromIDString(string s) => new(Convert.ToUInt64(s));    // For strings that are already IDs
    public override string ToString() => Value.ToString("X8");
}

public readonly struct ResourceTag : IIDTag
{
    public static ulong Hash(string text) => Crc32.Compute(text.ToLower(System.Globalization.CultureInfo.CurrentCulture));
    public static ulong Hash(ReadOnlySpan<byte> data) => throw new NotSupportedException("Generating ResourceIDs from direct bytes is not supported. Please convert your input to a string and use the Hash(string text) overload.");
}

public readonly struct SnrTag : IIDTag
{
    public static ulong Hash(string text) => Crc64.Compute(text.ToLower(System.Globalization.CultureInfo.CurrentCulture));
    public static ulong Hash(ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<byte> prefix = "Volatility_"u8;
        Span<byte> buf = stackalloc byte[prefix.Length + data.Length];
        prefix.CopyTo(buf);
        data.CopyTo(buf[prefix.Length..]);
        return Crc64.Compute(buf);
    }
}

public static class Crc32
{
    private static readonly uint[] Table = BuildTable();
    public static uint Compute(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        uint crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
            crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
        return ~crc;
    }
    private static uint[] BuildTable()
    {
        const uint poly = 0xEDB88320u;
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i;
            for (int j = 0; j < 8; j++)
                c = (c & 1) != 0 ? (poly ^ (c >> 1)) : (c >> 1);
            table[i] = c;
        }
        return table;
    }
}

public static class Crc64
{
    private static readonly ulong[] Table = BuildTable();

    public static ulong Compute(string s) =>
        Compute(Encoding.UTF8.GetBytes(s));

    public static ulong Compute(ReadOnlySpan<byte> data)
    {
        ulong crc = 0xFFFFFFFFFFFFFFFFul;
        foreach (var b in data)
            crc = Table[(byte)(crc ^ b)] ^ (crc >> 8);
        return ~crc;
    }

    private static ulong[] BuildTable()
    {
        const ulong poly = 0xC96C5795D7870F42ul;
        var table = new ulong[256];
        for (ulong i = 0; i < 256; i++)
        {
            ulong c = i;
            for (int j = 0; j < 8; j++)
                c = (c & 1) != 0 ? (poly ^ (c >> 1)) : (c >> 1);
            table[i] = c;
        }
        return table;
    }
}
