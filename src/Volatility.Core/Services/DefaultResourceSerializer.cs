using Volatility.Abstractions.Services;
using Volatility.Resources;

namespace Volatility.Services;

public sealed class DefaultResourceSerializer : IResourceSerializer
{
    private readonly IResourceFactory _resourceFactory;

    public DefaultResourceSerializer(IResourceFactory resourceFactory)
    {
        _resourceFactory = resourceFactory;
    }

    public Resource Deserialize(
        Stream stream,
        ResourceType resourceType,
        Platform platform,
        ResourceSerializationOptions options)
    {
        Resource resource = _resourceFactory.CreateResource(resourceType, platform, options.x64);

        if (options.AssetName != null)
        {
            resource.AssetName = options.AssetName;
        }
        if (options.ResourceID.HasValue)
        {
            resource.ResourceID = options.ResourceID.Value;
        }
        if (options.Unpacker.HasValue)
        {
            resource.Unpacker = options.Unpacker.Value;
        }

        Endian defaultEndian = ResolveLoadEndianness(resource, platform);
        Endian finalEndian = options.Endianness ?? defaultEndian;

        resource.LoadFromStream(stream, finalEndian, options.FileName, options.ResourceDBLookup);

        resource.PullAll();

        return resource;
    }

    public void Serialize(
        Resource resource,
        Stream stream,
        ResourceSerializationOptions options)
    {
        resource.PushAll();

        Endian finalEndian = options.Endianness ?? resource.ResourceEndian;

        using ResourceBinaryWriter writer = new(stream, finalEndian, leaveOpen: true);
        resource.WriteToStream(writer, finalEndian);
    }

    private static Endian ResolveLoadEndianness(Resource resource, Platform platform)
    {
        if (resource.ResourceEndian != Endian.Agnostic)
        {
            return resource.ResourceEndian;
        }

        return platform switch
        {
            Platform.BPR or Platform.TUB => Endian.LE,
            Platform.X360 or Platform.PS3 => Endian.BE,
            _ => Endian.Agnostic
        };
    }
}
