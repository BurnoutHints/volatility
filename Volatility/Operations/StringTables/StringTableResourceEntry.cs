namespace Volatility.Operations.StringTables;

public class StringTableResourceEntry
{
    public string Name { get; set; } = string.Empty;
    public List<string> Appearances { get; set; } = new();
}
