using Volatility.Abstractions.Messaging;

namespace Volatility.Messaging;

public sealed class NullMessageSink : IMessageSink
{
    public static NullMessageSink Instance { get; } = new();

    private NullMessageSink()
    {
    }

    public void Publish(in VolatilityMessage message)
    {
    }
}
