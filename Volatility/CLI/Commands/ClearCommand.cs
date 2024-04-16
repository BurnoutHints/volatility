namespace Volatility;

class ClearCommand : ICommand
{
    public void Execute()
    {
        Console.Clear();
    }

    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage()
    {
        Console.WriteLine
        (
            "Usage: clear" +
            "\nClears the console."
        );
    }
}
