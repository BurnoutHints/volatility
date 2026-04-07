using System.Runtime.Serialization;

using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal class LoadResourceOperation
{
    public async Task<Resource> ExecuteAsync(string sourceFile, ResourceType resourceType, Platform platform)
    {
        string yaml = await File.ReadAllTextAsync(sourceFile);

        Resource resource = ResourceFactory.CreateResource(resourceType, platform, string.Empty);

        Resource? result = (Resource?)ResourceYamlDeserializer.DeserializeResource(resource.GetType(), yaml);

        if (result is null)
        {
            throw new SerializationException();
        }

        result.ImportedFileName = sourceFile;
        return result;
    }
}
