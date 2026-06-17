using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Volatility.Abstractions.Services;

namespace Volatility.Resources;

public static class ResourceFactory
{
    private static readonly Dictionary<(ResourceType, Platform), ResourceRegistrationInfo> resourceCreators = CreateResourceCreators();

    public static Resource CreateResource(ResourceType resourceType, Platform platform, bool x64 = false)
    {
        ResourceRegistrationInfo registration = ResolveRegistration(resourceType, platform);
        Resource resource = registration.Activator();
        ApplyArchOption(resource, x64);

        if (registration.PullAll)
        {
            resource.PullAll();
        }

        return resource;
    }



    private static Dictionary<(ResourceType, Platform), ResourceRegistrationInfo> CreateResourceCreators()
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

    private static ResourceRegistrationInfo ResolveRegistration(ResourceType resourceType, Platform platform)
    {
        if (resourceCreators.TryGetValue((resourceType, platform), out ResourceRegistrationInfo? registration))
        {
            return registration;
        }

        throw new InvalidPlatformException($"The '{resourceType}' type is not supported for the '{platform}' platform.");
    }

    private static void ApplyArchOption(Resource resource, bool x64)
    {
        if (x64)
        {
            resource.SetResourceArch(Arch.x64);
        }
    }



    private static void AddRegisteredResource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TResource>(
        ResourceCreatorRegistry registry)
        where TResource : Resource
    {
        registry.AddRegistrations(typeof(TResource));
    }

    private sealed class ResourceCreatorRegistry
    {
        private readonly Dictionary<(ResourceType, Platform), ResourceRegistrationInfo> creators = new();

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
                    creators.Add(
                        (resourceType, platform),
                        new ResourceRegistrationInfo(
                            CreateActivator(resourceClass),
                            registration.EndianMapped,
                            registration.PullAll));
                }
            }
        }

        public Dictionary<(ResourceType, Platform), ResourceRegistrationInfo> Build()
        {
            return creators;
        }

        private static Func<Resource> CreateActivator(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type resourceClass)
        {
            ConstructorInfo? constructor = resourceClass.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new InvalidOperationException(
                    $"Resource class '{resourceClass.FullName}' must expose a parameterless constructor.");
            }

            return () => (Resource)constructor.Invoke([]);
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

    private sealed record ResourceRegistrationInfo(
        Func<Resource> Activator,
        bool EndianMapped,
        bool PullAll);
}
