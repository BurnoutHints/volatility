using System.Text;
using System.Xml.Linq;

using Newtonsoft.Json;

using static Volatility.Utilities.CgsIDUtilities;

namespace Volatility.CLI.Commands;

internal class ImportStringTableCommand : ICommand
{
    public string CommandToken => "ImportStringTable";
    public string CommandDescription => "Imports a entries from a file containing a ResourceStringTable.";
    public string CommandParameters => "[--endian=<le,be>] --path=<file path>";

    public string? Endian { get; set; }
    public string? ImportPath { get; set; }
    public bool Overwrite { get; set; }
    public bool Recursive { get; set; }

    public void Execute()
    {
        if (string.IsNullOrEmpty(ImportPath))
        {
            Console.WriteLine("Error: No import path specified! (--path)");
            return;
        }

        foreach (string filePath in ICommand.GetFilePathsInDirectory(ImportPath, ICommand.TargetFileType.Any, Recursive))
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            string fileContent = Encoding.UTF8.GetString(fileBytes);
            int startIndex = fileContent.IndexOf("<ResourceStringTable>");
            int endIndex = fileContent.IndexOf("</ResourceStringTable>") + "</ResourceStringTable>".Length;

            if (startIndex == -1 || endIndex == -1 || endIndex <= startIndex)
            {
                Console.WriteLine($"ResourceStringTable not found in {filePath}, skipping...");
                continue;
            }

            string xmlContent = fileContent.Substring(startIndex, endIndex - startIndex);

            if (string.IsNullOrEmpty(xmlContent)) 
            {
                Console.WriteLine($"Found ResourceStringTable {filePath} is malformed, skipping...");
                continue;
            }

            XDocument xmlDoc = XDocument.Parse(xmlContent);
            var entries = xmlDoc.Descendants("Resource")
                                .Select(x => new
                                {
                                    Id = Endian == "be" ? FlipCgsIDEndian((string)x.Attribute("id")) : (string)x.Attribute("id"),
                                    Type = (string)x.Attribute("type"),
                                    Name = (string)x.Attribute("name")
                                });

            if (entries.Count() == 0)
            {
                Console.WriteLine($"No entries found in ResourceStringTable {filePath}, skipping...");
                continue;
            }

            var groupedByType = entries.GroupBy(e => e.Type)
                                       .ToDictionary(g => g.Key, g => g.ToDictionary(e => e.Id, e => e.Name));

            string directoryPath = Path.Combine
            (
                Directory.GetCurrentDirectory(),
                "data",
                "ResourceDB"
            );

            Directory.CreateDirectory(directoryPath);

            foreach (var group in entries.GroupBy(e => e.Type))
            {
                string jsonFileName = Path.Combine(directoryPath, $"{group.Key}.json");
                SortedDictionary<string, string> existingData = new SortedDictionary<string, string>();

                if (File.Exists(jsonFileName))
                {
                    existingData = JsonConvert.DeserializeObject<SortedDictionary<string, string>>(File.ReadAllText(jsonFileName));
                }

                foreach (var entry in group)
                {
                    // Check for overwrite flag and duplicate entry
                    if (Overwrite || !existingData.ContainsKey(entry.Id))
                    {
                        existingData[entry.Id] = entry.Name;
                    }
                }

                Console.WriteLine($"Writing imported {group.Key} Resource string data from {Path.GetFileNameWithoutExtension(filePath)}...");
                string json = JsonConvert.SerializeObject(existingData, Formatting.Indented);
                File.WriteAllText(jsonFileName, json);
            }

            Console.WriteLine($"Finished importing all ResourceStringTable data into the ResourceDB.");
        }
    }

    public void SetArgs(Dictionary<string, object> args)
    {
        Endian = (args.TryGetValue("endian", out object? format) ? format as string : "le").ToLower();
        ImportPath = args.TryGetValue("path", out object? path) ? path as string : "";
        Overwrite = args.TryGetValue("overwrite", out var ow) && (bool)ow;
        Recursive = args.TryGetValue("recurse", out var re) && (bool)re;
    }

}
