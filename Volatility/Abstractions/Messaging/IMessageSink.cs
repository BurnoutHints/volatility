namespace Volatility.Abstractions.Messaging;

public interface IMessageSink
{
    void Publish(in VolatilityMessage message);
}
