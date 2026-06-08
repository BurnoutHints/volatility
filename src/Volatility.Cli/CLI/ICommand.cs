using static Volatility.Utilities.TypeUtilities;
using Volatility.CLI;

namespace Volatility;

internal interface ICommand
{
    static abstract string CommandToken { get; }
    static abstract string CommandDescription { get; }
    static abstract string CommandParameters { get; }

    Task Execute() => Task.CompletedTask;
    void SetArgs(Dictionary<string, object> args);
    public void ShowUsage() 
    {
        Type thisType = GetType();

        var token = GetStaticPropertyValue(thisType, "CommandToken");
        var parameters = GetStaticPropertyValue(thisType, "CommandParameters");
        var description = GetStaticPropertyValue(thisType, "CommandDescription");

        CLIMessageUtilities.Info(thisType.Name, $"Usage:\n   {token} {parameters}\n{description}");
    }
}
