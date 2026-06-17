using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface IResourceSerializer
{
    Resource Deserialize(
        Stream stream,
        ResourceType resourceType,
        Platform platform,
        ResourceSerializationOptions options);

    void Serialize(
        Resource resource,
        Stream stream,
        ResourceSerializationOptions options);
}
