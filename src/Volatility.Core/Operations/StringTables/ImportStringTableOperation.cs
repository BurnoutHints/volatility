using System.Text;
using System.Xml.Linq;
using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Operations;

using static Volatility.Utilities.DictUtilities;
using static Volatility.Utilities.ResourceIDUtilities;

namespace Volatility.Operations.StringTables;

internal sealed class ImportStringTableOperation
    : IOperation<ImportStringTableRequest, ImportStringTableResult>
{
    private readonly IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult> mergeOperation;
    private readonly IMessageSink messageSink;

    public ImportStringTableOperation(
        IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult> mergeOperation,
        IMessageSink messageSink)
    {
        this.mergeOperation = mergeOperation;
        this.messageSink = messageSink;
    }

    public async Task<OperationResult<ImportStringTableResult>> ExecuteAsync(
        ImportStringTableRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var results = await Task.WhenAll(request.FilePaths.Select(path =>
                ProcessFileAsync(path, request.Endian, request.Overwrite, request.Verbose, cancellationToken)));

            foreach (Dictionary<string, Dictionary<string, StringTableResourceEntry>> fileResult in results)
            {
                cancellationToken.ThrowIfCancellationRequested();

                OperationResult<MergeStringTableEntriesResult> mergeResult = await mergeOperation.ExecuteAsync(
                    new MergeStringTableEntriesRequest(request.Entries, fileResult, request.Overwrite),
                    progress: null,
                    cancellationToken);

                if (!mergeResult.Success)
                {
                    return OperationResultFactory.Failure<ImportStringTableResult>(
                        "import_string_table_merge_failed",
                        mergeResult.Issues.FirstOrDefault()?.Message ?? "Failed to merge string table entries.",
                        nameof(ImportStringTableOperation));
                }
            }

            progress?.Report(new OperationProgress("import-string-table", 1.0, null));
            return OperationResultFactory.Success(new ImportStringTableResult(request.Entries));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return OperationResultFactory.Failure<ImportStringTableResult>(
                "import_string_table_failed",
                ex.Message,
                nameof(ImportStringTableOperation));
        }
    }

    private async Task<Dictionary<string, Dictionary<string, StringTableResourceEntry>>> ProcessFileAsync(
        string filePath,
        string endian,
        bool overwrite,
        bool verbose,
        CancellationToken cancellationToken)
    {
        var entriesByType = new Dictionary<string, Dictionary<string, StringTableResourceEntry>>(StringComparer.OrdinalIgnoreCase);
        string fileName = Path.GetFileName(filePath)!;
        string text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(filePath, cancellationToken));

        int start = text.IndexOf("<ResourceStringTable>", StringComparison.Ordinal);
        int end = text.IndexOf("</ResourceStringTable>", StringComparison.Ordinal) + "</ResourceStringTable>".Length;
        if (start < 0 || end <= start)
        {
            if (verbose)
            {
                messageSink.Verbose(
                    $"Skipping (no table): {fileName}",
                    MessageCategory.StringTable,
                    nameof(ImportStringTableOperation));
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
                    messageSink.Verbose(
                        $"Found {entry.Type} entry in {Path.GetFileName(filePath)} - {entry.Name}",
                        MessageCategory.StringTable,
                        nameof(ImportStringTableOperation));
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

public sealed record ImportStringTableRequest(
    IReadOnlyList<string> FilePaths,
    Dictionary<string, Dictionary<string, StringTableResourceEntry>> Entries,
    string Endian,
    bool Overwrite,
    bool Verbose) : IOperationRequest;

public sealed record ImportStringTableResult(
    Dictionary<string, Dictionary<string, StringTableResourceEntry>> Entries);
