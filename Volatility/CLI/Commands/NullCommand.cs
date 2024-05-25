namespace Volatility.CLI.Commands;

internal class NullCommand : ICommand
{
    public string CommandToken => "";
    public string CommandDescription => "";
    public string CommandParameters => "";

    public async Task Execute() { }
    
    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage() { }
}