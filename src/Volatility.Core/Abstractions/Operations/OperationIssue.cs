using Volatility.Abstractions.Messaging;

namespace Volatility.Abstractions.Operations;

public sealed record OperationIssue(MessageSeverity Severity, string Code, string Message, string? Source);
