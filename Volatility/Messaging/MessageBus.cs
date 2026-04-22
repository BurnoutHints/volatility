using Volatility.Abstractions.Messaging;

namespace Volatility.Messaging;

public sealed class MessageBus : IMessageBus
{
    private readonly object syncRoot = new();
    private readonly List<IMessageSink> sinks = [];
    private readonly List<HandlerSubscription> handlers = [];

    public void Publish(in VolatilityMessage message)
    {
        IMessageSink[] sinkSnapshot;
        HandlerSubscription[] handlerSnapshot;

        lock (syncRoot)
        {
            sinkSnapshot = sinks.ToArray();
            handlerSnapshot = handlers.ToArray();
        }

        foreach (IMessageSink sink in sinkSnapshot)
        {
            sink.Publish(in message);
        }

        foreach (HandlerSubscription handler in handlerSnapshot)
        {
            if (message.Severity >= handler.MinimumSeverity)
            {
                handler.Handler(message);
            }
        }
    }

    public IDisposable Subscribe(IMessageSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        lock (syncRoot)
        {
            sinks.Add(sink);
        }

        return new Subscription(() => UnsubscribeSink(sink));
    }

    public IDisposable Subscribe(MessageSeverity minSeverity, Action<VolatilityMessage> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        HandlerSubscription subscription = new(minSeverity, handler);

        lock (syncRoot)
        {
            handlers.Add(subscription);
        }

        return new Subscription(() => UnsubscribeHandler(subscription));
    }

    private void UnsubscribeSink(IMessageSink sink)
    {
        lock (syncRoot)
        {
            sinks.Remove(sink);
        }
    }

    private void UnsubscribeHandler(HandlerSubscription subscription)
    {
        lock (syncRoot)
        {
            handlers.Remove(subscription);
        }
    }

    private sealed record HandlerSubscription(MessageSeverity MinimumSeverity, Action<VolatilityMessage> Handler);

    private sealed class Subscription(Action disposeAction) : IDisposable
    {
        private Action? disposeAction = disposeAction;

        public void Dispose()
        {
            Interlocked.Exchange(ref disposeAction, null)?.Invoke();
        }
    }
}
