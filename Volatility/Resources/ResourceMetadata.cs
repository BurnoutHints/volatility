using System.Collections.Concurrent;
using System.Reflection;

namespace Volatility.Resources;

[Flags]
public enum RegistrationPlatforms
{
    None = 0,
    BPR = 1 << 0,
    TUB = 1 << 1,
    X360 = 1 << 2,
    PS3 = 1 << 3,
    Agnostic = 1 << 4,
    All = BPR | TUB | X360 | PS3,
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class ResourceDefinitionAttribute : Attribute
{
    public ResourceDefinitionAttribute(ResourceType resourceType)
    {
        ResourceType = resourceType;
    }

    public ResourceType ResourceType { get; }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ResourceRegistrationAttribute : Attribute
{
    public ResourceRegistrationAttribute(RegistrationPlatforms platforms)
    {
        Platforms = platforms;
    }

    public RegistrationPlatforms Platforms { get; }
    public bool EndianMapped { get; init; }
    public bool PullAll { get; init; }
}

internal static class ResourceMetadata
{
    private static readonly ConcurrentDictionary<Type, ResourceType> ResourceTypes = new();

    public static ResourceType GetResourceType(Type resourceClass)
    {
        return ResourceTypes.GetOrAdd(resourceClass, static type =>
        {
            ResourceDefinitionAttribute? definition = type.GetCustomAttribute<ResourceDefinitionAttribute>(inherit: true);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    $"Resource type metadata is missing for '{type.FullName}'. Add [ResourceDefinition(...)] to the resource or its base class.");
            }

            return definition.ResourceType;
        });
    }
}
