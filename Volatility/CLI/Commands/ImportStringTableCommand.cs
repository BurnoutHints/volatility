using System.Text;
using System.Xml.Linq;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using static Volatility.Utilities.EnvironmentUtilities;
using static Volatility.Utilities.ResourceIDUtilities;
using static Volatility.Utilities.DictUtilities;

namespace Volatility.CLI.Commands;

internal class ImportStringTableCommand : ICommand
{
    public static string CommandToken => "ImportStringTable";
    public static string CommandDescription => "Imports entries into the ResourceDB from files containing a ResourceStringTable.";
    public static string CommandParameters => "[--verbose] [--endian=<le,be>] --path=<file path>";

    public string? Endian { get; set; }
    public string? ImportPath { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }
    public bool Verbose { get; set; }

    private class ResourceEntry
    {
        public string Name { get; set; } = "";
        public List<string> Appearances { get; set; } = new();
    }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        var filePaths = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Any, Recursive);
        if (filePaths.Length == 0)
        {
            Console.WriteLine("Error: No files or folders found within the specified path!");
            return;
        }

        Console.WriteLine($"Importing data from ResourceStringTables into the ResourceDB... this may take a while!");

        string directoryPath = GetEnvironmentDirectory(EnvironmentDirectory.ResourceDB);
        Directory.CreateDirectory(directoryPath);
        string yamlFile = Path.Combine(directoryPath, "ResourceDB.yaml");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var allEntries = File.Exists(yamlFile)
            ? deserializer.Deserialize<Dictionary<string, Dictionary<string, ResourceEntry>>>(await File.ReadAllTextAsync(yamlFile))
              ?? new Dictionary<string, Dictionary<string, ResourceEntry>>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, Dictionary<string, ResourceEntry>>(StringComparer.OrdinalIgnoreCase);

        var results = await Task.WhenAll(filePaths.Select(ProcessFileAsync));
        foreach (var fileResult in results)
        {
            foreach (var typePair in fileResult)
            {
                var typeDict = allEntries.GetOrCreate(typePair.Key, () => new Dictionary<string, ResourceEntry>());
                foreach (var resPair in typePair.Value)
                {
                    if (!typeDict.TryGetValue(resPair.Key, out var existing))
                    {
                        typeDict[resPair.Key] = resPair.Value;
                    }
                    else
                    {
                        if (Overwrite)
                            existing.Name = resPair.Value.Name;
                        foreach (var fn in resPair.Value.Appearances)
                            if (!existing.Appearances.Contains(fn))
                                existing.Appearances.Add(fn);
                    }
                }
            }
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        string yaml = serializer.Serialize(allEntries);
        await File.WriteAllTextAsync(yamlFile, yaml, Encoding.UTF8);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Console.WriteLine($"Finished importing all ResourceDB (v2) data at {yamlFile}.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Endian = (args.TryGetValue("endian", out object? format) ? format as string : "le").ToLower();
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
        Verbose = args.TryGetValue("verbose", out var ve) && (bool)ve;
    }

    private async Task<Dictionary<string, Dictionary<string, ResourceEntry>>> ProcessFileAsync(string filePath)
    {
        var entriesByType = new Dictionary<string, Dictionary<string, ResourceEntry>>(StringComparer.OrdinalIgnoreCase);
        var fileName = Path.GetFileName(filePath)!;
        var text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(filePath));

        int start = text.IndexOf("<ResourceStringTable>");
        int end = text.IndexOf("</ResourceStringTable>") + "</ResourceStringTable>".Length;
        if (start < 0 || end <= start)
        {
            if (Verbose) Console.WriteLine($"Skipping (no table): {fileName}");
            return entriesByType;
        }

        XDocument xmlDoc = XDocument.Parse(text[start..end]);
        var entries = xmlDoc.Descendants("Resource")
            .Select(x => new
            {
                Id = Endian == "be"
                    ? FlipResourceIDEndian((string)x.Attribute("id")!)
                    : (string)x.Attribute("id")!,
                Type = (string)x.Attribute("type")!,
                Name = (string)x.Attribute("name")!
            }).ToList();

        foreach (var e in entries)
        {
            var dict = entriesByType.GetOrCreate(e.Type, () => new Dictionary<string, ResourceEntry>());
            if (!dict.TryGetValue(e.Id, out var existing))
            {
                dict[e.Id] = new ResourceEntry { Name = e.Name, Appearances = { fileName } };
                if (Verbose) Console.WriteLine($"Found {e.Type} entry in {Path.GetFileName(filePath)} - {e.Name}");
            }
            else
            {
                if (Overwrite)
                    existing.Name = e.Name;
                if (!existing.Appearances.Contains(fileName))
                    existing.Appearances.Add(fileName);
            }
        }

        return entriesByType;
    }

    public ImportStringTableCommand() { }
}
