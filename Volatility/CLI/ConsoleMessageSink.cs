using Volatility.Abstractions.Messaging;

namespace Volatility.CLI;

public sealed class ConsoleMessageSink(bool verbose = true) : IMessageSink
{
    public void Publish(in VolatilityMessage message)
    {
        if (message.Severity == MessageSeverity.Verbose && !verbose)
        {
            return;
        }

        ConsoleColor previousColor = Console.ForegroundColor;
        ConsoleColor? messageColor = message.Severity switch
        {
            MessageSeverity.Error => ConsoleColor.Red,
            MessageSeverity.Warning => ConsoleColor.Yellow,
            MessageSeverity.Success => ConsoleColor.Green,
            MessageSeverity.Verbose => ConsoleColor.DarkGray,
            _ => null
        };

        if (messageColor.HasValue)
        {
            Console.ForegroundColor = messageColor.Value;
        }

        Console.WriteLine(message.Text);

        if (messageColor.HasValue)
        {
            Console.ForegroundColor = previousColor;
        }
    }
}
