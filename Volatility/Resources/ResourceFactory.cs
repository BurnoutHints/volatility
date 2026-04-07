
namespace Volatility.Resources;

public static class ResourceFactory
{
    private static readonly Dictionary<(ResourceType, Platform), Func<string, Resource>> resourceCreators = new()
    {
        // Texture resources
        { (ResourceType.Texture, Platform.BPR), path => {
            var resource = new TextureBPR(path);
            resource.PullAll();
            return resource;
        } },
        { (ResourceType.Texture, Platform.TUB), path => {
            var resource = new TexturePC(path);
            resource.PullAll();
            return resource;
        } },
        { (ResourceType.Texture, Platform.X360), path => {
            var resource = new TextureX360(path);
            resource.PullAll();
            return resource;
        } },
        { (ResourceType.Texture, Platform.PS3), path => {
            var resource = new TexturePS3(path);
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

        // EnvironmentKeyframe resources
        { (ResourceType.EnvironmentKeyframe, Platform.BPR), path => new EnvironmentKeyframe(path, Endian.LE) },
        { (ResourceType.EnvironmentKeyframe, Platform.TUB), path => new EnvironmentKeyframe(path, Endian.LE) },
        { (ResourceType.EnvironmentKeyframe, Platform.X360), path => new EnvironmentKeyframe(path, Endian.BE) },
        { (ResourceType.EnvironmentKeyframe, Platform.PS3), path => new EnvironmentKeyframe(path, Endian.BE) },

        // EnvironmentTimeline resources
        { (ResourceType.EnvironmentTimeLine, Platform.BPR), path => new EnvironmentTimeline(path, Endian.LE) },
        { (ResourceType.EnvironmentTimeLine, Platform.TUB), path => new EnvironmentTimeline(path, Endian.LE) },
        { (ResourceType.EnvironmentTimeLine, Platform.X360), path => new EnvironmentTimeline(path, Endian.BE) },
        { (ResourceType.EnvironmentTimeLine, Platform.PS3), path => new EnvironmentTimeline(path, Endian.BE) },

        // SnapshotData resources
        { (ResourceType.SnapshotData, Platform.BPR), path => new SnapshotData(path, Endian.LE) },
        { (ResourceType.SnapshotData, Platform.TUB), path => new SnapshotData(path, Endian.LE) },
        { (ResourceType.SnapshotData, Platform.X360), path => new SnapshotData(path, Endian.BE) },
        { (ResourceType.SnapshotData, Platform.PS3), path => new SnapshotData(path, Endian.BE) },

        // AptData resources
        { (ResourceType.AptData, Platform.BPR), path => new AptData(path, Endian.LE) },
        { (ResourceType.AptData, Platform.TUB), path => new AptData(path, Endian.LE) },
        { (ResourceType.AptData, Platform.X360), path => new AptData(path, Endian.BE) },
        { (ResourceType.AptData, Platform.PS3), path => new AptData(path, Endian.BE) },

        // Shader resources
        { (ResourceType.Shader, Platform.Agnostic), path => new ShaderBase(path) },
        { (ResourceType.Shader, Platform.TUB), path => new ShaderPC(path) },
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
