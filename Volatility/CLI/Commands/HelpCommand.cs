namespace Volatility.CLI.Commands;

internal class HelpCommand : ICommand
{
    public string CommandToken => "help";
    public string CommandDescription => "Displays all available commands & parameters for individual commands.";
    public string CommandParameters => "[command]";

    public async Task Execute()
    {
        Console.WriteLine("TODO: List all commands (is there a good way to do this?)");
    }

    public void SetArgs(Dictionary<string, object> args) { }
}