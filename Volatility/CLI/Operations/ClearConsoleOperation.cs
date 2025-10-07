using System.Threading;

namespace Volatility.CLI.Operations;

internal sealed class ClearConsoleOperation : ICommandOperation<ClearConsoleOperationParameters>
{
    public Task ExecuteAsync(ClearConsoleOperationParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters.PauseBeforeClear)
        {
            Console.ReadKey(intercept: true);
        }

        Console.Clear();
        return Task.CompletedTask;
    }
}

internal sealed record ClearConsoleOperationParameters(bool PauseBeforeClear)
{
    public static ClearConsoleOperationParameters Default { get; } = new(false);
}
