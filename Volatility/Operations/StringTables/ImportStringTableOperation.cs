using System.Text;
using System.Xml.Linq;
using Volatility.Abstractions.Operations;
using Volatility.Operations;

using static Volatility.Utilities.DictUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Operations.StringTables;

internal sealed class ImportStringTableOperation
{
    private readonly IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult> mergeOperation;

    public ImportStringTableOperation(
        IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult> mergeOperation)
    {
        this.mergeOperation = mergeOperation;
    }

    public async Task ExecuteAsync(
        IEnumerable<string> filePaths,
        Dictionary<string, Dictionary<string, StringTableResourceEntry>> entries,
        string endian,
        bool overwrite,
        bool verbose)
    {
        var results = await Task.WhenAll(filePaths.Select(path => ProcessFileAsync(path, endian, overwrite, verbose)));

        foreach (Dictionary<string, Dictionary<string, StringTableResourceEntry>> fileResult in results)
        {
            OperationResult<MergeStringTableEntriesResult> mergeResult = await mergeOperation.ExecuteAsync(
                new MergeStringTableEntriesRequest(entries, fileResult, overwrite),
                progress: null,
                cancellationToken: CancellationToken.None);

            if (!mergeResult.Success)
            {
                throw OperationResultFactory.CreateException(mergeResult, "Failed to merge string table entries.");
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static async Task<Dictionary<string, Dictionary<string, StringTableResourceEntry>>> ProcessFileAsync(
        string filePath,
        string endian,
        bool overwrite,
        bool verbose)
    {
        var entriesByType = new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
        string fileName = Path.GetFileName(filePath)!;
        string text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(filePath));

        int start = text.IndexOf("<ResourceStringTable>");
        int end = text.IndexOf("</ResourceStringTable>") + "</ResourceStringTable>".Length;
        if (start < 0 || end <= start)
        {
            if (verbose)
            {
                Console.WriteLine($"Skipping (no table): {fileName}");
            }

            return entriesByType;
        }

        XDocument xmlDoc = XDocument.Parse(text[start..end]);
        var resourceEntries = xmlDoc.Descendants("Resource")
            .Select(x => new
            {
                Id = endian == "be"
                    ? FlipResourceIDEndian((string)x.Attribute("id")!)
                    : (string)x.Attribute("id")!,
                Type = (string)x.Attribute("type")!,
                Name = (string)x.Attribute("name")!
            }).ToList();

        foreach (var entry in resourceEntries)
        {
            var dict = entriesByType.GetOrCreate(entry.Type, () => new Dictionary<string, StringTableResourceEntry>());
            if (!dict.TryGetValue(entry.Id, out StringTableResourceEntry? existing))
            {
                dict[entry.Id] = new StringTableResourceEntry { Name = entry.Name, Appearances = { fileName } };
                if (verbose)
                {
                    Console.WriteLine($"Found {entry.Type} entry in {Path.GetFileName(filePath)} - {entry.Name}");
                }
            }
            else
            {
                if (overwrite)
                {
                    existing.Name = entry.Name;
                }

                if (!existing.Appearances.Contains(fileName))
                {
                    existing.Appearances.Add(fileName);
                }
            }
        }

        return entriesByType;
    }
}
