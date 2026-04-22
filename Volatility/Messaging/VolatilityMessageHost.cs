using Volatility.Abstractions.Messaging;

namespace Volatility.Messaging;

public static class VolatilityMessageHost
{
    private static IMessageBus bus = new MessageBus();

    public static IMessageBus Bus => bus;
    public static IMessageSink Sink => bus;

    public static void Reset(IMessageBus? messageBus = null)
    {
        bus = messageBus ?? new MessageBus();
    }
}
