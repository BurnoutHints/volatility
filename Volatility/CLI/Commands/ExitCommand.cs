namespace Volatility.CLI.Commands;

internal class ExitCommand : ICommand
{
    public string CommandToken => "exit";
    public string CommandDescription => "Exits the application.";
    public string CommandParameters => "";

    public void Execute()
    {
        Console.WriteLine("Exiting Volatility...");
        Environment.Exit(0);
    }

    public void SetArgs(Dictionary<string, object> args) { }
}