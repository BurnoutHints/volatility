
using Volatility.Resource.Renderable;
using Volatility.Resource.Splicer;
using Volatility.Resource.Texture;

namespace Volatility.Resource;

public static class ResourceFactory
{
    private static readonly Dictionary<(ResourceType, Platform), Func<string, Resource>> resourceCreators = new()
    {
        // Texture resources
        { (ResourceType.Texture, Platform.BPR), path => {
            var resource = new TextureHeaderBPR(path);
            resource.PullAll();
            return resource;
        } },
        { (ResourceType.Texture, Platform.TUB), path => {
            var resource = new TextureHeaderPC(path);
            resource.PullAll();
            return resource;
        } },
        { (ResourceType.Texture, Platform.X360), path => {
            var resource = new TextureHeaderX360(path);
            resource.PullAll();
            return resource;
        } },
        { (ResourceType.Texture, Platform.PS3), path => {
            var resource = new TextureHeaderPS3(path);
            resource.PullAll();
            return resource;
        } },

        // Splicer resources
        { (ResourceType.Splicer, Platform.BPR), path => new SplicerLE(path) },
        { (ResourceType.Splicer, Platform.TUB), path => new SplicerLE(path) },
        { (ResourceType.Splicer, Platform.X360), path => new SplicerBE(path) },
        { (ResourceType.Splicer, Platform.PS3), path => new SplicerBE(path) },

        // Renderable resources
        { (ResourceType.Renderable, Platform.BPR), path => new RenderableBPR(path) },
        { (ResourceType.Renderable, Platform.TUB), path => new RenderablePC(path) },
        { (ResourceType.Renderable, Platform.X360), path => new RenderableX360(path) },
        { (ResourceType.Renderable, Platform.PS3), path => new RenderablePS3(path) },
    };

    public static Resource CreateResource(ResourceType resourceType, Platform platform, string filePath)
    {
        var key = (resourceType, platform);
        if (resourceCreators.TryGetValue(key, out var creator))
        {
            return creator(filePath);
        }
        else
        {
            throw new InvalidPlatformException($"The '{resourceType}' type is not supported for the '{platform}' platform.");
        }
    }
}