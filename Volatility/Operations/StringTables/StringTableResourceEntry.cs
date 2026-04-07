namespace Volatility.Operations.StringTables;

internal class StringTableResourceEntry
{
    public string Name { get; set; } = string.Empty;
    public List<string> Appearances { get; set; } = new();
}
