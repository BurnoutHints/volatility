using Volatility.CLI.Operations;

namespace Volatility.CLI.Commands;

internal sealed class ClearCommand : ICommand
{
    public static string CommandToken => "clear";
    public static string CommandDescription => "Clears the console.";
    public static string CommandParameters => "[--pause]";

    private readonly ClearConsoleOperation _operation;
    private ClearConsoleOperationParameters _parameters = ClearConsoleOperationParameters.Default;

    public ClearCommand()
        : this(new ClearConsoleOperation())
    {
    }

    internal ClearCommand(ClearConsoleOperation operation)
    {
        _operation = operation;
    }

    public Task Execute() => _operation.ExecuteAsync(_parameters);

    public void SetArgs(Dictionary<string, object> args)
    {
        var pauseBeforeClear = false;
        if (args.TryGetValue("pause", out var value))
        {
            if (value is bool flag)
            {
                pauseBeforeClear = flag;
            }
            else if (value is string text && bool.TryParse(text, out var parsed))
            {
                pauseBeforeClear = parsed;
            }
        }
        _parameters = new ClearConsoleOperationParameters(pauseBeforeClear);
    }
}
