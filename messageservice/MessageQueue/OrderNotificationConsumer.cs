using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;

public class OrderNotificationConsumer : BackgroundService
{
    private readonly ILogger<OrderNotificationConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public OrderNotificationConsumer(ILogger<OrderNotificationConsumer> logger, IConfiguration config)
    {
        _logger = logger;

        _connection = new ConnectionFactory
        {
            HostName = config["RabbitMQ:host"],
            Port = int.Parse(config["RabbitMQ:port"] ?? "5672"),
            VirtualHost = "/",
        }.CreateConnection();

        _channel = _connection.CreateModel();
        _channel.QueueDeclare(queue: "notifyQueue", durable: true, exclusive: false, autoDelete: false, arguments: null);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) => ProcessMessage(ea);
        _channel.BasicConsume(queue: "notifyQueue", autoAck: false, consumer: consumer);

        return Task.CompletedTask;
    }

    private void ProcessMessage(BasicDeliverEventArgs ea)
    {
        var raw = Encoding.UTF8.GetString(ea.Body.ToArray());
        _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

        var notification = JsonConvert.DeserializeObject<OrderNotification>(raw);
        if (notification is null)
        {
            _logger.LogWarning("Received a message that could not be deserialized as OrderNotification.");
            return;
        }

        string subject = $"Your recent order with us: #{notification.OrderGuid}";
        string body =
            $"Account #: {notification.UserGuid}\n" +
            $"Order #:   {notification.OrderGuid}\n\n" +
            $"Dear {notification.Name},\n\n" +
            $"Thank you for your recent order with us!\n\n" +
            $"{notification.Message}\n\n" +
            $"Sincerely,\n\nTHE MANAGEMENT";

        _logger.LogInformation("Notification received. OrderGuid: {OrderGuid}, Recipient: {Email}.",
            notification.OrderGuid, notification.Email);

        SendEmail(notification.Email, subject, body);
    }

    private void SendEmail(string? recipient, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(recipient))
        {
            _logger.LogWarning("No recipient email address on notification — skipping send.");
            return;
        }

        // Using Ethereal (free fake SMTP for testing): https://ethereal.email
        // Emails are captured on Ethereal's site and never actually delivered.
        // Replace these credentials with your own Ethereal account for local testing.
        var client = new SmtpClient("smtp.ethereal.email", 587)
        {
            Credentials = new NetworkCredential("ashtyn35@ethereal.email", "uhvaTk4j4pBCTfWj49"),
            EnableSsl = true
        };

        var mail = new MailMessage("noreply@menus.com", recipient, subject, body);
        client.Send(mail);

        _logger.LogInformation("Email sent to {Recipient}.", recipient);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}