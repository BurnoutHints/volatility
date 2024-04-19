namespace Volatility;

internal class ExitCommand : ICommand
{
    public void Execute()
    {
        Console.WriteLine("Exiting Volatility...");
        Environment.Exit(0);
    }
    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: exit" +
            "\nExits the application."
        );
    }
}