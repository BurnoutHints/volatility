namespace Volatility.Abstractions.Messaging;

public interface IMessageBus : IMessageSink
{
    IDisposable Subscribe(IMessageSink sink);
    IDisposable Subscribe(MessageSeverity minSeverity, Action<VolatilityMessage> handler);
}
