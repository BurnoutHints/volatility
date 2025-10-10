namespace Volatility.Operations.StringTables;

internal class MergeStringTableEntriesOperation
{
    public void Execute(Dictionary<string, Dictionary<string, StringTableResourceEntry>> target, Dictionary<string, Dictionary<string, StringTableResourceEntry>> source, bool overwrite)
    {
        foreach ((string typeKey, Dictionary<string, StringTableResourceEntry> resourceEntries) in source)
        {
            if (!target.TryGetValue(typeKey, out Dictionary<string, StringTableResourceEntry>? typeDict))
            {
                target[typeKey] = new Dictionary<string, StringTableResourceEntry>(resourceEntries, StringComparer.OrdinalIgnoreCase);
                continue;
            }

            foreach ((string resourceKey, StringTableResourceEntry entry) in resourceEntries)
            {
                if (!typeDict.TryGetValue(resourceKey, out StringTableResourceEntry? existing))
                {
                    typeDict[resourceKey] = entry;
                    continue;
                }

                if (overwrite)
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
    }
}
