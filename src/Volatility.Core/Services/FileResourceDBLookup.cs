using Newtonsoft.Json;
using Volatility.Abstractions.Services;
using Volatility.Operations.StringTables;
using Volatility.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Volatility.Services;

public sealed class FileResourceDBLookup(IPathProvider pathProvider) : IResourceDBLookup
{
    private readonly object sync = new();
    private string? cachedPath;
    private DateTime cachedWriteTimeUtc;
    private Dictionary<string, string> cachedLookup = new(StringComparer.OrdinalIgnoreCase);

    public string GetNameByResourceId(string id)
    {
        string normalizedId = NormalizeId(id);
        IReadOnlyDictionary<string, string> lookup = LoadLookup();
        return lookup.TryGetValue(normalizedId, out string? value) ? value : string.Empty;
    }

    public string GetNameByResourceId(ResourceID id)
    {
        return GetNameByResourceId(id.ToString());
    }

    private IReadOnlyDictionary<string, string> LoadLookup()
    {
        lock (sync)
        {
            string? resourceDBPath = ResolveResourceDBPath();
            if (resourceDBPath == null)
            {
                cachedPath = null;
                cachedLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                cachedWriteTimeUtc = default;
                return cachedLookup;
            }

            DateTime writeTimeUtc = File.GetLastWriteTimeUtc(resourceDBPath);
            if (string.Equals(cachedPath, resourceDBPath, StringComparison.OrdinalIgnoreCase) &&
                cachedWriteTimeUtc == writeTimeUtc)
            {
                return cachedLookup;
            }

            cachedLookup = LoadEntries(resourceDBPath);
            cachedPath = resourceDBPath;
            cachedWriteTimeUtc = writeTimeUtc;
            return cachedLookup;
        }
    }

    private string? ResolveResourceDBPath()
    {
        string resourceDBDirectory = pathProvider.GetDirectory(VolatilityPathLocation.ResourceDB);
        string yamlPath = Path.Combine(resourceDBDirectory, "ResourceDB.yaml");
        if (File.Exists(yamlPath))
        {
            return yamlPath;
        }

        string jsonPath = Path.Combine(resourceDBDirectory, "ResourceDB.json");
        return File.Exists(jsonPath) ? jsonPath : null;
    }

    private static Dictionary<string, string> LoadEntries(string resourceDBPath)
    {
        return string.Equals(Path.GetExtension(resourceDBPath), ".yaml", StringComparison.OrdinalIgnoreCase)
            ? LoadYamlEntries(resourceDBPath)
            : LoadJsonEntries(resourceDBPath);
    }

    private static Dictionary<string, string> LoadYamlEntries(string yamlPath)
    {
        string content = File.ReadAllText(yamlPath);
        Dictionary<string, Dictionary<string, StringTableResourceEntry>>? typedEntries = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build()
            .Deserialize<Dictionary<string, Dictionary<string, StringTableResourceEntry>>>(content);

        Dictionary<string, string> lookup = new(StringComparer.OrdinalIgnoreCase);
        if (typedEntries == null)
        {
            return lookup;
        }

        foreach (Dictionary<string, StringTableResourceEntry> typeEntries in typedEntries.Values)
        {
            foreach ((string resourceId, StringTableResourceEntry entry) in typeEntries)
            {
                string normalizedId = NormalizeId(resourceId);
                if (!lookup.ContainsKey(normalizedId) && !string.IsNullOrWhiteSpace(entry.Name))
                {
                    lookup[normalizedId] = entry.Name;
                }
            }
        }

        return lookup;
    }

    private static Dictionary<string, string> LoadJsonEntries(string jsonPath)
    {
        Dictionary<string, string>? data =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(jsonPath));

        return data != null
            ? new Dictionary<string, string>(
                data.ToDictionary(entry => NormalizeId(entry.Key), entry => entry.Value),
                StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeId(string id)
    {
        return id
            .Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();
    }
}
