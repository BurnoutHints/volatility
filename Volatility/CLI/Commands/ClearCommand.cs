namespace Volatility.CLI.Commands;

internal class ClearCommand : ICommand
{
    public string CommandToken => "clear";
    public string CommandDescription => "Clears the console.";
    public string CommandParameters => "";
    public async Task Execute() => Console.Clear();
    public void SetArgs(Dictionary<string, object> args) { }
}
