
using Volatility.Resources.InstanceList;
using Volatility.Resources.Model;
using Volatility.Resources.Renderable;
using Volatility.Resources.Splicer;
using Volatility.Resources.Texture;
using Volatility.Resources.EnvironmentKeyframe;

namespace Volatility.Resources;

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

        // InstanceList resources
        { (ResourceType.InstanceList, Platform.BPR), path => new InstanceListLE(path) },
        { (ResourceType.InstanceList, Platform.TUB), path => new InstanceListLE(path) },
        { (ResourceType.InstanceList, Platform.X360), path => new InstanceListBE(path) },
        { (ResourceType.InstanceList, Platform.PS3), path => new InstanceListBE(path) },

        // Model resources
        { (ResourceType.Model, Platform.BPR), path => new ModelLE(path) },
        { (ResourceType.Model, Platform.TUB), path => new ModelLE(path) },
        { (ResourceType.Model, Platform.X360), path => new ModelBE(path) },
        { (ResourceType.Model, Platform.PS3), path => new ModelBE(path) },

        // Model resources
        { (ResourceType.EnvironmentKeyframe, Platform.BPR), path => new EnvironmentKeyframeLE(path) },
        { (ResourceType.EnvironmentKeyframe, Platform.TUB), path => new EnvironmentKeyframeLE(path) },
        { (ResourceType.EnvironmentKeyframe, Platform.X360), path => new EnvironmentKeyframeBE(path) },
        { (ResourceType.EnvironmentKeyframe, Platform.PS3), path => new EnvironmentKeyframeBE(path) },
    };

    public static Resource CreateResource(ResourceType resourceType, Platform platform, string filePath)
    {
        Console.WriteLine($"Constructing {platform} {resourceType} resource property data...");

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