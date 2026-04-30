using Volatility.CLI;

namespace Volatility.CLI.Commands;

internal class ExitCommand : ICommand
{
    public static string CommandToken => "exit";
    public static string CommandDescription => "Exits the application.";
    public static string CommandParameters => "";

    public async Task Execute()
    {
        CLIMessageUtilities.Info<ExitCommand>("Exiting Volatility...");
        Environment.Exit(0);
    }

    public void SetArgs(Dictionary<string, object> args) { }

    public ExitCommand() { }
}
