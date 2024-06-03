namespace Volatility.CLI.Commands;

internal class NullCommand : ICommand
{
    public static string CommandToken => "";
    public string CommandDescription => "";
    public static string CommandParameters => "";

    public async Task Execute() { }
    
    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage() { }
}