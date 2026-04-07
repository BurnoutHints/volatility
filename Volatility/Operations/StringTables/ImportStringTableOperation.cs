using System.Text;
using System.Xml.Linq;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using static Volatility.Utilities.ResourceIDUtilities;
using static Volatility.Utilities.DictUtilities;

namespace Volatility.Operations.StringTables;

internal class ImportStringTableOperation
{
    private readonly MergeStringTableEntriesOperation mergeOperation;

    public ImportStringTableOperation(MergeStringTableEntriesOperation mergeOperation)
    {
        this.mergeOperation = mergeOperation;
    }

    public async Task ExecuteAsync(IEnumerable<string> filePaths, Dictionary<string, Dictionary<string, StringTableResourceEntry>> entries, string endian, bool overwrite, bool verbose, string yamlFile)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var results = await Task.WhenAll(filePaths.Select(path => ProcessFileAsync(path, endian, overwrite, verbose)));

        foreach (var fileResult in results)
        {
            mergeOperation.Execute(entries, fileResult, overwrite);
        }

        string yaml = serializer.Serialize(entries);
        await File.WriteAllTextAsync(yamlFile, yaml, Encoding.UTF8);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private async Task<Dictionary<string, Dictionary<string, StringTableResourceEntry>>> ProcessFileAsync(string filePath, string endian, bool overwrite, bool verbose)
    {
        var entriesByType = new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
        string fileName = Path.GetFileName(filePath)!;
        string text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(filePath));

        int start = text.IndexOf("<ResourceStringTable>");
        int end = text.IndexOf("</ResourceStringTable>") + "</ResourceStringTable>".Length;
        if (start < 0 || end <= start)
        {
            if (verbose) Console.WriteLine($"Skipping (no table): {fileName}");
            return entriesByType;
        }

        XDocument xmlDoc = XDocument.Parse(text[start..end]);
        var entries = xmlDoc.Descendants("Resource")
            .Select(x => new
            {
                Id = endian == "be"
                    ? FlipResourceIDEndian((string)x.Attribute("id")!)
                    : (string)x.Attribute("id")!,
                Type = (string)x.Attribute("type")!,
                Name = (string)x.Attribute("name")!
            }).ToList();

        foreach (var e in entries)
        {
            var dict = entriesByType.GetOrCreate(e.Type, () => new Dictionary<string, StringTableResourceEntry>());
            if (!dict.TryGetValue(e.Id, out StringTableResourceEntry? existing))
            {
                dict[e.Id] = new StringTableResourceEntry { Name = e.Name, Appearances = { fileName } };
                if (verbose) Console.WriteLine($"Found {e.Type} entry in {Path.GetFileName(filePath)} - {e.Name}");
            }
            else
            {
                if (overwrite)
                    existing.Name = e.Name;
                if (!existing.Appearances.Contains(fileName))
                    existing.Appearances.Add(fileName);
            }
        }

        return entriesByType;
    }
}
