namespace Volatility.Abstractions.Messaging;

public readonly record struct VolatilityMessage(
    MessageSeverity Severity,
    MessageCategory Category,
    string Text,
    string? Source = null,
    IReadOnlyDictionary<string, object?>? Data = null);
