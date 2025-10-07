using System.Threading;

namespace Volatility.CLI.Operations;

internal interface ICommandOperation<in TParameters>
{
    Task ExecuteAsync(TParameters parameters, CancellationToken cancellationToken = default);
}
