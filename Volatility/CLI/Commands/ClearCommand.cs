namespace Volatility.CLI.Commands;

internal class ClearCommand : ICommand
{
    public static string CommandToken => "clear";
    public static string CommandDescription => "Clears the console.";
    public static string CommandParameters => "";
    public async Task Execute() => Console.Clear();
    public void SetArgs(Dictionary<string, object> args) { }
}
