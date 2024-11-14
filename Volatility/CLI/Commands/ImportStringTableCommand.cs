using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;

using Newtonsoft.Json;

using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.CLI.Commands;

internal class ImportStringTableCommand : ICommand
{
    public static string CommandToken => "ImportStringTable";
    public static string CommandDescription => "Imports entries into the ResourceDB from files containing a ResourceStringTable.";
    public static string CommandParameters => "[--endian=<le,be>] --path=<file path>";

    public string? Endian { get; set; }
    public string? ImportPath { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }

    public async Task Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        var filePaths = ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Any, Recursive);
        var allEntries = new Dictionary<string, Dictionary<string, string>>();

        if (filePaths.Length == 0)
        {
            Console.WriteLine("Error: No files or folders found within the specified path!");
            return;
        }

        List<Task<Dictionary<string, Dictionary<string, string>>>> tasks = new();

        foreach (string filePath in filePaths)
        {
            tasks.Add(ProcessFileAsync(filePath));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var fileResult in results)
        {
            foreach (var typeEntry in fileResult)
            {
                if (!allEntries.ContainsKey(typeEntry.Key))
                {
                    allEntries[typeEntry.Key] = new Dictionary<string, string>();
                }

                foreach (var resource in typeEntry.Value)
                {
                    allEntries[typeEntry.Key][resource.Key] = resource.Value; // Assumes overwrite logic or add logic
                }
            }
        }

        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "ResourceDB");
        Directory.CreateDirectory(directoryPath);

        foreach (var group in allEntries)
        {
            string jsonFileName = Path.Combine(directoryPath, $"{group.Key}.json");
            string json = JsonConvert.SerializeObject(group.Value, Formatting.Indented);
            await File.WriteAllTextAsync(jsonFileName, json);
            Console.WriteLine($"Resource string data for {group.Key} written to file.");
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Console.WriteLine($"Finished importing all ResourceStringTable data into the ResourceDB.");
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Endian = (args.TryGetValue("endian", out object? format) ? format as string : "le").ToLower();
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
    }

    private async Task<Dictionary<string, Dictionary<string, string>>> ProcessFileAsync(string filePath)
    {
        Dictionary<string, Dictionary<string, string>> entriesByType = new Dictionary<string, Dictionary<string, string>>();
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        string fileContent = Encoding.UTF8.GetString(fileBytes);
        int startIndex = fileContent.IndexOf("<ResourceStringTable>");
        int endIndex = fileContent.IndexOf("</ResourceStringTable>") + "</ResourceStringTable>".Length;

        if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
        {
            Console.WriteLine($"ResourceStringTable not found in {filePath}, skipping...");
            return entriesByType;
        }

        string xmlContent = fileContent.Substring(startIndex, endIndex - startIndex);

        if (string.IsNullOrEmpty(xmlContent))
        {
            Console.WriteLine($"Found ResourceStringTable {filePath} is malformed, skipping...");
            return entriesByType;
        }

        XDocument xmlDoc = XDocument.Parse(xmlContent);
        var entries = xmlDoc.Descendants("Resource")
                            .Select(x => new
                            {
                                Id = Endian == "be" ? FlipResourceIDEndian((string)x.Attribute("id")) : (string)x.Attribute("id"),
                                Type = (string)x.Attribute("type"),
                                Name = (string)x.Attribute("name")
                            }).ToList();

        if (!entries.Any())
        {
            Console.WriteLine($"No entries found in ResourceStringTable {filePath}, skipping...");
            return entriesByType;
        }

        foreach (var entry in entries)
        {
            if (!entriesByType.ContainsKey(entry.Type))
            {
                entriesByType[entry.Type] = new Dictionary<string, string>();
            }

            if (Overwrite || !entriesByType[entry.Type].ContainsKey(entry.Id))
            {
                entriesByType[entry.Type][entry.Id] = entry.Name;
            }
        }

        return entriesByType;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ImportStringTableCommand))]
    public ImportStringTableCommand() { }
}
