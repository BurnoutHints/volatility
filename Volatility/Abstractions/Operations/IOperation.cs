namespace Volatility.Abstractions.Operations;

public interface IOperation<in TRequest, TResult>
    where TRequest : IOperationRequest
{
    Task<OperationResult<TResult>> ExecuteAsync(
        TRequest request,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken);
}
