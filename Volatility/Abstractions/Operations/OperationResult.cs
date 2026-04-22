namespace Volatility.Abstractions.Operations;

public readonly record struct OperationResult<T>(
    bool Success,
    T? Value,
    IReadOnlyList<OperationIssue> Issues);
