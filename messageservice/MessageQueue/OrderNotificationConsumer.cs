using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;

public class OrderNotificationConsumer : BackgroundService
{
    private readonly ILogger<OrderNotificationConsumer> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderNotificationConsumer(
        ILogger<OrderNotificationConsumer> logger,
        IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:host"] ?? "localhost",
            Port = int.TryParse(_config["RabbitMQ:port"], out var port) ? port : 5672,
            UserName = _config["RabbitMQ:username"] ?? "appuser",
            Password = _config["RabbitMQ:password"] ?? "apppass",
            VirtualHost = "/",
            AutomaticRecoveryEnabled = true
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "notifyQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel was not initialized.");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            await ProcessMessageAsync(ea, stoppingToken);
        };

        await _channel.BasicConsumeAsync(
            queue: "notifyQueue",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        var raw = Encoding.UTF8.GetString(ea.Body.ToArray());
        var notification = JsonConvert.DeserializeObject<OrderNotification>(raw);

        if (notification is null)
        {
            _logger.LogWarning("Received a message that could not be deserialized as OrderNotification.");
            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            return;
        }

        string subject = $"Your recent order with us: #{notification.OrderGuid}";
        string body =
            $"Customer #: {notification.CustomerGuid}\n" +
            $"Order #:   {notification.OrderGuid}\n\n" +
            $"Dear {notification.Name},\n\n" +
            $"Thank you for your recent order with us!\n\n" +
            $"{notification.Message}\n\n" +
            $"Sincerely,\n\nTHE MANAGEMENT";

        _logger.LogInformation(
            "Notification received. OrderGuid: {OrderGuid}, Recipient: {Email}.",
            notification.OrderGuid,
            notification.Email);

        SendEmail(notification.Email, subject, body);

        await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
    }

    private void SendEmail(string? recipient, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("No recipient email address on notification — skipping send.");
            return;
        }

        using var client = new SmtpClient("smtp.ethereal.email", 587)
        {
            Credentials = new NetworkCredential("ashtyn35@ethereal.email", "uhvaTk4j4pBCTfWj49"),
            EnableSsl = true
        };

        using var mail = new MailMessage("noreply@menus.com", recipient, subject, body);
        client.Send(mail);

        _logger.LogInformation("Email sent to {Recipient}.", recipient);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}