using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;

namespace Volatility.Operations;

internal static class OperationResultFactory
{
    public static OperationResult<T> Success<T>(T value, params OperationIssue[] issues)
    {
        return new OperationResult<T>(true, value, issues);
    }

    public static OperationResult<T> Failure<T>(
        string code,
        string message,
        string source,
        MessageSeverity severity = MessageSeverity.Error)
    {
        return new OperationResult<T>(
            false,
            default,
            [new OperationIssue(severity, code, message, source)]);
    }

    public static InvalidOperationException CreateException<T>(OperationResult<T> result, string fallbackMessage)
    {
        return new InvalidOperationException(result.Issues.FirstOrDefault()?.Message ?? fallbackMessage);
    }
}
