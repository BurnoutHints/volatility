using Volatility.Operations.StringTables;

namespace Volatility.Abstractions.Services;

public interface IStringTableStore
{
    Task WriteYamlAsync(string yamlFile, Dictionary<string, Dictionary<string, StringTableResourceEntry>> entries);
    Task<Dictionary<string, string>> LoadJsonAsync(string jsonFile);
    Task WriteJsonAsync(string jsonFile, Dictionary<string, string> entries);
    void MergeLegacyEntries(
        Dictionary<string, string> target,
        Dictionary<string, Dictionary<string, StringTableResourceEntry>> source,
        bool overwrite);
}
