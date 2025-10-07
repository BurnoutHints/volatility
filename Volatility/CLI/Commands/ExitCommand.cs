using Volatility.CLI.Operations;

namespace Volatility.CLI.Commands;

internal sealed class ExitCommand : ICommand
{
    public static string CommandToken => "exit";
    public static string CommandDescription => "Exits the application.";
    public static string CommandParameters => "[--code=<exit code>] [--message=<message>]";

    private readonly ExitOperation _operation;
    private ExitOperationParameters _parameters = ExitOperationParameters.Default;

    public ExitCommand()
        : this(new ExitOperation())
    {
    }

    internal ExitCommand(ExitOperation operation)
    {
        _operation = operation;
    }

    public Task Execute() => _operation.ExecuteAsync(_parameters);

    public void SetArgs(Dictionary<string, object> args)
    {
        var message = args.TryGetValue("message", out var messageValue) ? messageValue?.ToString() : null;

        var exitCode = ExitOperationParameters.Default.ExitCode;
        if (args.TryGetValue("code", out var codeValue))
        {
            if (codeValue is string codeText && int.TryParse(codeText, out var parsed))
            {
                exitCode = parsed;
            }
            else if (codeValue is int numericValue)
            {
                exitCode = numericValue;
            }
            else if (codeValue is bool flag && flag)
            {
                exitCode = 1;
            }
        }

        _parameters = new ExitOperationParameters(message, exitCode);
    }
}