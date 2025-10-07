using System.Threading;

namespace Volatility.CLI.Operations;

internal sealed class ExitOperation : ICommandOperation<ExitOperationParameters>
{
    public Task ExecuteAsync(ExitOperationParameters parameters, CancellationToken cancellationToken = default)
    {
        var message = string.IsNullOrWhiteSpace(parameters.Message)
            ? ExitOperationParameters.Default.Message
            : parameters.Message;

        if (!string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine(message);
        }

        Environment.Exit(parameters.ExitCode);
        return Task.CompletedTask;
    }
}

internal sealed record ExitOperationParameters(string? Message, int ExitCode)
{
    public static ExitOperationParameters Default { get; } = new("Exiting Volatility...", 0);
}
