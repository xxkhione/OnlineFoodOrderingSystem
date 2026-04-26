public interface IOrderNotificationProducer
{
    void SendMessage<T>(T message);
}