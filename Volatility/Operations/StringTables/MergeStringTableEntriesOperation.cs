using Volatility.Abstractions.Operations;
using Volatility.Operations;

namespace Volatility.Operations.StringTables;

internal sealed class MergeStringTableEntriesOperation
    : IOperation<MergeStringTableEntriesRequest, MergeStringTableEntriesResult>
{
    public Task<OperationResult<MergeStringTableEntriesResult>> ExecuteAsync(
        MergeStringTableEntriesRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            foreach ((string typeKey, Dictionary<string, StringTableResourceEntry> resourceEntries) in request.Source)
            {
                if (!request.Target.TryGetValue(typeKey, out Dictionary<string, StringTableResourceEntry>? typeDict))
                {
                    request.Target[typeKey] = new Dictionary<string, StringTableResourceEntry>(resourceEntries, StringComparer.OrdinalIgnoreCase);
                    continue;
                }

                foreach ((string resourceKey, StringTableResourceEntry entry) in resourceEntries)
                {
                    if (!typeDict.TryGetValue(resourceKey, out StringTableResourceEntry? existing))
                    {
                        typeDict[resourceKey] = entry;
                        continue;
                    }

                    if (request.Overwrite)
                    {
                        existing.Name = entry.Name;
                    }

                    foreach (string appearance in entry.Appearances)
                    {
                        if (!existing.Appearances.Contains(appearance))
                        {
                            existing.Appearances.Add(appearance);
                        }
                    }
                }
            }

            progress?.Report(new OperationProgress("merge-string-table-entries", 1.0, null));
            return Task.FromResult(OperationResultFactory.Success(new MergeStringTableEntriesResult(request.Target)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResultFactory.Failure<MergeStringTableEntriesResult>(
                "merge_string_table_entries_failed",
                ex.Message,
                nameof(MergeStringTableEntriesOperation)));
        }
    }

    public void Execute(
        Dictionary<string, Dictionary<string, StringTableResourceEntry>> target,
        Dictionary<string, Dictionary<string, StringTableResourceEntry>> source,
        bool overwrite)
    {
        OperationResult<MergeStringTableEntriesResult> result = ExecuteAsync(
            new MergeStringTableEntriesRequest(target, source, overwrite),
            progress: null,
            cancellationToken: CancellationToken.None).GetAwaiter().GetResult();

        if (!result.Success)
        {
            throw OperationResultFactory.CreateException(result, "Failed to merge string table entries.");
        }
    }
}

internal sealed record MergeStringTableEntriesRequest(
    Dictionary<string, Dictionary<string, StringTableResourceEntry>> Target,
    Dictionary<string, Dictionary<string, StringTableResourceEntry>> Source,
    bool Overwrite) : IOperationRequest;

internal sealed record MergeStringTableEntriesResult(
    Dictionary<string, Dictionary<string, StringTableResourceEntry>> Entries);
