
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
        { (ResourceType.Splicer, Platform.BPR), path => new Splicer(path, Endian.LE) },
        { (ResourceType.Splicer, Platform.TUB), path => new Splicer(path, Endian.LE) },
        { (ResourceType.Splicer, Platform.X360), path => new Splicer(path, Endian.BE) },
        { (ResourceType.Splicer, Platform.PS3), path => new Splicer(path, Endian.BE) },

        // Renderable resources
        { (ResourceType.Renderable, Platform.BPR), path => new RenderableBPR(path) },
        { (ResourceType.Renderable, Platform.TUB), path => new RenderablePC(path) },
        { (ResourceType.Renderable, Platform.X360), path => new RenderableX360(path) },
        { (ResourceType.Renderable, Platform.PS3), path => new RenderablePS3(path) },

        // InstanceList resources
        { (ResourceType.InstanceList, Platform.BPR), path => new InstanceList(path, Endian.LE) },
        { (ResourceType.InstanceList, Platform.TUB), path => new InstanceList(path, Endian.LE) },
        { (ResourceType.InstanceList, Platform.X360), path => new InstanceList(path, Endian.BE) },
        { (ResourceType.InstanceList, Platform.PS3), path => new InstanceList(path, Endian.BE) },

        // Model resources
        { (ResourceType.Model, Platform.BPR), path => new Model(path, Endian.LE) },
        { (ResourceType.Model, Platform.TUB), path => new Model(path, Endian.LE) },
        { (ResourceType.Model, Platform.X360), path => new Model(path, Endian.BE) },
        { (ResourceType.Model, Platform.PS3), path => new Model(path, Endian.BE) },

        // Model resources
        { (ResourceType.EnvironmentKeyframe, Platform.BPR), path => new EnvironmentKeyframe(path, Endian.LE) },
        { (ResourceType.EnvironmentKeyframe, Platform.TUB), path => new EnvironmentKeyframe(path, Endian.LE) },
        { (ResourceType.EnvironmentKeyframe, Platform.X360), path => new EnvironmentKeyframe(path, Endian.BE) },
        { (ResourceType.EnvironmentKeyframe, Platform.PS3), path => new EnvironmentKeyframe(path, Endian.BE) },
    };

    public static Resource CreateResource(ResourceType resourceType, Platform platform, string filePath, bool x64 = false)
    {
        Console.WriteLine($"Constructing {platform} {resourceType} resource property data...");

        var key = (resourceType, platform);
        if (resourceCreators.TryGetValue(key, out var creator))
        {
            var output = creator(filePath);
            if (x64)
                output.SetResourceArch(Arch.x64);
            return output;
        }
        else
        {
            throw new InvalidPlatformException($"The '{resourceType}' type is not supported for the '{platform}' platform.");
        }
    }
}