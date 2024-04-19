namespace Volatility;

internal class HelpCommand : ICommand
{
    public void Execute()
    {
        Console.WriteLine("TODO: List all commands (is there a good way to do this?)");
        ShowUsage();
    }

    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: help [command]" +
            "\nDisplays all available commands & parameters for individual commands."
        );
    }
}