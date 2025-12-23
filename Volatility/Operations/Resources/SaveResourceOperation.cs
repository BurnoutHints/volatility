using YamlDotNet.Serialization;

using Volatility.Resources;
using Volatility.Utilities;

namespace Volatility.Operations.Resources;

internal class SaveResourceOperation
{
    private readonly ISerializer serializer;

    public SaveResourceOperation()
    {
        serializer = new SerializerBuilder()
            .DisableAliases()
            .WithTypeInspector(inner => new IncludeFieldsTypeInspector(inner))
            .WithTypeConverter(new ResourceYamlTypeConverter())
            .WithTypeConverter(new StrongIDYamlTypeConverter())
            .WithTypeConverter(new StringEnumYamlTypeConverter())
            .Build();
    }

    public async Task ExecuteAsync(Resource resource, string filePath)
    {
        string? directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string serializedString = serializer.Serialize(resource);
        await File.WriteAllTextAsync(filePath, serializedString);
    }
}
