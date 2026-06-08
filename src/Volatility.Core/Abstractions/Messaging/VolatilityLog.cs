namespace Volatility.Abstractions.Messaging;

public static class VolatilityLog
{
    public static void Verbose(
        this IMessageSink? sink,
        string text,
        MessageCategory category = MessageCategory.General,
        string? source = null,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        PublishMessage(sink, MessageSeverity.Verbose, category, text, source, data);
    }

    public static void Info(
        this IMessageSink? sink,
        string text,
        MessageCategory category = MessageCategory.General,
        string? source = null,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        PublishMessage(sink, MessageSeverity.Info, category, text, source, data);
    }

    public static void Success(
        this IMessageSink? sink,
        string text,
        MessageCategory category = MessageCategory.General,
        string? source = null,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        PublishMessage(sink, MessageSeverity.Success, category, text, source, data);
    }

    public static void Warning(
        this IMessageSink? sink,
        string text,
        MessageCategory category = MessageCategory.General,
        string? source = null,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        PublishMessage(sink, MessageSeverity.Warning, category, text, source, data);
    }

    public static void Error(
        this IMessageSink? sink,
        string text,
        MessageCategory category = MessageCategory.General,
        string? source = null,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        PublishMessage(sink, MessageSeverity.Error, category, text, source, data);
    }

    private static void PublishMessage(
        IMessageSink? sink,
        MessageSeverity severity,
        MessageCategory category,
        string text,
        string? source,
        IReadOnlyDictionary<string, object?>? data)
    {
        if (sink is null)
        {
            return;
        }

        VolatilityMessage message = new(severity, category, text, source, data);
        sink.Publish(in message);
    }
}
