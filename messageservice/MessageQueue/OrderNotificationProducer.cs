using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

public class OrderNotificationProducer : IOrderNotificationProducer, IAsyncDisposable
{
    private readonly Task<IConnection> _connectionTask;

    public OrderNotificationProducer(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:host"] ?? "localhost",
            Port = int.TryParse(config["RabbitMQ:port"], out var port) ? port : 5672,
            VirtualHost = "/",
            AutomaticRecoveryEnabled = true
        };

        _connectionTask = factory.CreateConnectionAsync();
    }

    public async Task SendMessage<T>(T message)
    {
        var connection = await _connectionTask;
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "notifyQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var json = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "notifyQueue",
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connectionTask.IsCompletedSuccessfully)
        {
            var connection = await _connectionTask;
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }
    }
}