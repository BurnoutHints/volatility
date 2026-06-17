using Volatility.Abstractions.Services;
using Volatility.Resources;

namespace Volatility.Services;

public sealed class DefaultResourceFactory : IResourceFactory
{
    public Resource CreateResource(ResourceType resourceType, Platform platform, bool x64 = false)
    {
        return ResourceFactory.CreateResource(resourceType, platform, x64);
    }
}
