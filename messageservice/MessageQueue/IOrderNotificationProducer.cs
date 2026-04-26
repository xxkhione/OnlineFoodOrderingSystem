public interface IOrderNotificationProducer
{
    Task SendMessage<T>(T message);
}