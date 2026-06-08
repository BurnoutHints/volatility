namespace Volatility.Abstractions.Operations;

public sealed record OperationProgress(string Stage, double? Completion, string? Detail);
