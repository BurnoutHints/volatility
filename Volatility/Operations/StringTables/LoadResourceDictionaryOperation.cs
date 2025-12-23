using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Volatility.Operations.StringTables;

internal class LoadResourceDictionaryOperation
{
    private readonly IDeserializer deserializer;

    public LoadResourceDictionaryOperation()
    {
        deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public async Task<Dictionary<string, Dictionary<string, StringTableResourceEntry>>> ExecuteAsync(string yamlFile)
    {
        if (!File.Exists(yamlFile))
        {
            return new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
        }

        string content = await File.ReadAllTextAsync(yamlFile);

        Dictionary<string, Dictionary<string, StringTableResourceEntry>>? result = deserializer.Deserialize<Dictionary<string, Dictionary<string, StringTableResourceEntry>>>(content);

        return result ?? new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
    }
}
