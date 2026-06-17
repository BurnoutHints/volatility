namespace Volatility.CLI.Commands;

internal class NullCommand : ICommand
{
    public static string CommandToken => "";
    public static string CommandDescription => "";
    public static string CommandParameters => "";

    public Task Execute() => Task.CompletedTask;
    
    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage() { }

    public NullCommand() { }
}