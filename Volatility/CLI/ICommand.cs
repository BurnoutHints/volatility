namespace Volatility;

internal interface ICommand
{
    string CommandToken { get; }
    string CommandDescription { get; }
    string CommandParameters { get; }
    void Execute();
    void SetArgs(Dictionary<string, object> args);
    void ShowUsage() => Console.WriteLine($"Usage: {CommandToken} {CommandParameters}\n{CommandDescription}");
}