using Volatility.Abstractions.Messaging;
using Volatility.Abstractions.Operations;
using Volatility.Messaging;

namespace Volatility.CLI;

internal static class CLIMessageUtilities
{
    public static void PublishIssues(IEnumerable<OperationIssue> issues, MessageCategory category = MessageCategory.CLI)
    {
        foreach (OperationIssue issue in issues)
        {
            PublishIssue(issue, category);
        }
    }

    public static void PublishIssue(OperationIssue issue, MessageCategory category = MessageCategory.CLI)
    {
        Publish(issue.Severity, issue.Message, category, issue.Source);
    }

    public static void Verbose<TSource>(string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Verbose, message, category, typeof(TSource).Name);
    }

    public static void Info<TSource>(string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Info, message, category, typeof(TSource).Name);
    }

    public static void Success<TSource>(string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Success, message, category, typeof(TSource).Name);
    }

    public static void Warning<TSource>(string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Warning, message, category, typeof(TSource).Name);
    }

    public static void Error<TSource>(string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Error, message, category, typeof(TSource).Name);
    }

    public static void Verbose(string source, string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Verbose, message, category, source);
    }

    public static void Info(string source, string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Info, message, category, source);
    }

    public static void Success(string source, string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Success, message, category, source);
    }

    public static void Warning(string source, string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Warning, message, category, source);
    }

    public static void Error(string source, string message, MessageCategory category = MessageCategory.CLI)
    {
        Publish(MessageSeverity.Error, message, category, source);
    }

    private static void Publish(MessageSeverity severity, string message, MessageCategory category, string? source)
    {
        switch (severity)
        {
            case MessageSeverity.Verbose:
                VolatilityMessageHost.Sink.Verbose(message, category, source);
                break;
            case MessageSeverity.Info:
                VolatilityMessageHost.Sink.Info(message, category, source);
                break;
            case MessageSeverity.Success:
                VolatilityMessageHost.Sink.Success(message, category, source);
                break;
            case MessageSeverity.Warning:
                VolatilityMessageHost.Sink.Warning(message, category, source);
                break;
            default:
                VolatilityMessageHost.Sink.Error(message, category, source);
                break;
        }
    }
}
