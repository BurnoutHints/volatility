namespace Volatility;

internal class NullCommand : ICommand
{
    public void Execute() { }
    
    public void SetArgs(Dictionary<string, object> args) { }

    public void ShowUsage() { }
}