namespace Volatility;

internal interface ICommand
{
    void Execute();
    void SetArgs(Dictionary<string, object> args);
    void ShowUsage();
}