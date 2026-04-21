using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Volatility.Resources;

public static class ResourceFactory
{
    private static readonly Dictionary<(ResourceType, Platform), Func<string, Resource>> resourceCreators = CreateResourceCreators();

    private static Dictionary<(ResourceType, Platform), Func<string, Resource>> CreateResourceCreators()
    {
        ResourceCreatorRegistry registry = new();

        AddRegisteredResource<BinaryResource>(registry);
        AddRegisteredResource<TextureBPR>(registry);
        AddRegisteredResource<TexturePC>(registry);
        AddRegisteredResource<TextureX360>(registry);
        AddRegisteredResource<TexturePS3>(registry);
        AddRegisteredResource<Splicer>(registry);
        AddRegisteredResource<RenderableBPR>(registry);
        AddRegisteredResource<RenderablePC>(registry);
        AddRegisteredResource<RenderableX360>(registry);
        AddRegisteredResource<RenderablePS3>(registry);
        AddRegisteredResource<InstanceList>(registry);
        AddRegisteredResource<Model>(registry);
        AddRegisteredResource<EnvironmentKeyframe>(registry);
        AddRegisteredResource<EnvironmentTimeline>(registry);
        AddRegisteredResource<SnapshotData>(registry);
        AddRegisteredResource<AttribSysVault>(registry);
        AddRegisteredResource<StreamedDeformationSpec>(registry);
        AddRegisteredResource<AptData>(registry);
        AddRegisteredResource<GuiPopup>(registry);
        AddRegisteredResource<ShaderBase>(registry);
        AddRegisteredResource<ShaderPC>(registry);
        AddRegisteredResource<ShaderProgramBufferBPR>(registry);

        return registry.Build();
    }

    public static Resource CreateResource(ResourceType resourceType, Platform platform, string filePath, bool x64 = false)
    {
        Console.WriteLine($"Constructing {platform} {resourceType} resource property data...");

        var key = (resourceType, platform);
        if (resourceCreators.TryGetValue(key, out var creator))
        {
            Resource output = creator(filePath);
            if (x64)
                output.SetResourceArch(Arch.x64);
            return output;
        }

        throw new InvalidPlatformException($"The '{resourceType}' type is not supported for the '{platform}' platform.");
    }

    private static void AddRegisteredResource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TResource>(
        ResourceCreatorRegistry registry)
        where TResource : Resource
    {
        registry.AddRegistrations(typeof(TResource));
    }

    private sealed class ResourceCreatorRegistry
    {
        private readonly Dictionary<(ResourceType, Platform), Func<string, Resource>> _creators = new();

        public void AddCreator(ResourceType resourceType, Platform platform, Func<string, Resource> creator)
        {
            _creators.Add((resourceType, platform), creator);
        }

        public void Add<TResource>(
            ResourceType resourceType,
            Platform platform,
            Func<string, TResource> creator,
            Action<TResource>? afterCreate = null)
            where TResource : Resource
        {
            AddCreator(resourceType, platform, path =>
            {
                TResource resource = creator(path);
                afterCreate?.Invoke(resource);
                return resource;
            });
        }

        public void AddRegistrations(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type resourceClass)
        {
            ResourceType resourceType = ResourceMetadata.GetResourceType(resourceClass);
            ResourceRegistrationAttribute[] registrations = resourceClass
                .GetCustomAttributes<ResourceRegistrationAttribute>(inherit: false)
                .ToArray();

            foreach (ResourceRegistrationAttribute registration in registrations)
            {
                foreach (Platform platform in ExpandPlatforms(registration.Platforms))
                {
                    Func<string, Resource> creator = registration.EndianMapped
                        ? CreateEndianMappedCreator(resourceClass, platform)
                        : CreatePathCreator(resourceClass);

                    if (registration.PullAll)
                    {
                        creator = WrapWithPullAll(creator);
                    }

                    AddCreator(resourceType, platform, creator);
                }
            }
        }

        public Dictionary<(ResourceType, Platform), Func<string, Resource>> Build()
        {
            return _creators;
        }

        private static Func<string, Resource> CreatePathCreator(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type resourceClass)
        {
            ConstructorInfo? stringCtor = resourceClass.GetConstructor([typeof(string)]);
            if (stringCtor != null)
            {
                return path => (Resource)stringCtor.Invoke([path]);
            }

            ConstructorInfo? stringEndianCtor = resourceClass.GetConstructor([typeof(string), typeof(Endian)]);
            if (stringEndianCtor != null)
            {
                return path => (Resource)stringEndianCtor.Invoke([path, Endian.Agnostic]);
            }

            throw new InvalidOperationException(
                $"Could not find a usable string constructor for resource class '{resourceClass.FullName}'.");
        }

        private static Func<string, Resource> CreateEndianMappedCreator(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type resourceClass,
            Platform platform)
        {
            if (platform == Platform.Agnostic)
            {
                throw new InvalidOperationException(
                    $"Resource class '{resourceClass.FullName}' cannot use endian-mapped registration with Platform.Agnostic.");
            }

            ConstructorInfo? constructor = resourceClass.GetConstructor([typeof(string), typeof(Endian)]);
            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Resource class '{resourceClass.FullName}' must expose a (string path, Endian endianness) constructor for endian-mapped registration.");
            }

            Endian endianness = platform switch
            {
                Platform.BPR or Platform.TUB => Endian.LE,
                Platform.X360 or Platform.PS3 => Endian.BE,
                _ => throw new InvalidOperationException($"No default endianness mapping exists for platform '{platform}'."),
            };

            return path => (Resource)constructor.Invoke([path, endianness]);
        }

        private static Func<string, Resource> WrapWithPullAll(Func<string, Resource> creator)
        {
            return path =>
            {
                Resource resource = creator(path);
                resource.PullAll();
                return resource;
            };
        }

        private static IEnumerable<Platform> ExpandPlatforms(RegistrationPlatforms platforms)
        {
            if ((platforms & RegistrationPlatforms.Agnostic) != 0)
            {
                yield return Platform.Agnostic;
            }

            if ((platforms & RegistrationPlatforms.BPR) != 0)
            {
                yield return Platform.BPR;
            }

            if ((platforms & RegistrationPlatforms.TUB) != 0)
            {
                yield return Platform.TUB;
            }

            if ((platforms & RegistrationPlatforms.X360) != 0)
            {
                yield return Platform.X360;
            }

            if ((platforms & RegistrationPlatforms.PS3) != 0)
            {
                yield return Platform.PS3;
            }
        }
    }
}
