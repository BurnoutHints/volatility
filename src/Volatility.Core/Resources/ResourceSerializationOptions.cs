using Volatility.Abstractions.Services;

namespace Volatility.Resources;

public sealed class ResourceSerializationOptions
{
    public string? FileName { get; set; }
    public string? AssetName { get; set; }
    public ResourceID? ResourceID { get; set; }
    public Unpacker? Unpacker { get; set; }
    public IResourceDBLookup? ResourceDBLookup { get; set; }
    public bool x64 { get; set; }
    public Endian? Endianness { get; set; }
}
