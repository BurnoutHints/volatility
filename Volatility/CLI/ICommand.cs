namespace Volatility;

interface ICommand
{
    void Execute();
    void SetArgs(Dictionary<string, object> args);
    void ShowUsage();
}