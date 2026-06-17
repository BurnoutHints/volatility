using System.Text;
using Newtonsoft.Json;
using Volatility.Abstractions.Services;
using Volatility.Operations.StringTables;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Volatility.Services;

public sealed class FileStringTableStore : IStringTableStore
{
    public async Task WriteYamlAsync(string yamlFile, Dictionary<string, Dictionary<string, StringTableResourceEntry>> entries)
    {
        string yaml = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build()
            .Serialize(entries);

        await File.WriteAllTextAsync(yamlFile, yaml, Encoding.UTF8);
    }

    public async Task<Dictionary<string, string>> LoadJsonAsync(string jsonFile)
    {
        if (!File.Exists(jsonFile))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        string content = await File.ReadAllTextAsync(jsonFile);
        Dictionary<string, string>? result = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);

        return result != null
            ? new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task WriteJsonAsync(string jsonFile, Dictionary<string, string> entries)
    {
        string json = JsonConvert.SerializeObject(entries, Formatting.Indented);
        await File.WriteAllTextAsync(jsonFile, json, Encoding.UTF8);
    }

    public void MergeLegacyEntries(
        Dictionary<string, string> target,
        Dictionary<string, Dictionary<string, StringTableResourceEntry>> source,
        bool overwrite)
    {
        foreach (Dictionary<string, StringTableResourceEntry> resourceEntries in source.Values)
        {
            foreach ((string resourceKey, StringTableResourceEntry entry) in resourceEntries)
            {
                string normalizedKey = resourceKey.Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

                if (!target.ContainsKey(normalizedKey) || overwrite)
                {
                    target[normalizedKey] = entry.Name;
                }
            }
        }
    }
}
