using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

public class OrderNotificationProducer : IOrderNotificationProducer, IDisposable
{
    private readonly IConnection _connection;

    public OrderNotificationProducer(IConfiguration config)
    {
        _connection = new ConnectionFactory
        {
            HostName = config["RabbitMQ:host"],
            Port = int.Parse(config["RabbitMQ:port"] ?? "5672"),
        }.CreateConnection();
    }

    public void SendMessage<T>(T message)
    {
        using var channel = _connection.CreateModel();
        channel.QueueDeclare(queue: "notifyQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);

        var json = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(json);
        channel.BasicPublish(exchange: "", routingKey: "notifyQueue", basicProperties: null, body: body);
    }

    public void Dispose() => _connection?.Dispose();
}