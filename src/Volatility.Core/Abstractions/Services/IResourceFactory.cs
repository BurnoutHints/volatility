using Volatility.Resources;

namespace Volatility.Abstractions.Services;

public interface IResourceFactory
{
    Resource CreateResource(ResourceType resourceType, Platform platform, bool x64 = false);

    Resource LoadResource(
        ResourceType resourceType,
        Platform platform,
        string filePath,
        IResourceDBLookup? resourceDBLookup,
        bool x64 = false);
}
