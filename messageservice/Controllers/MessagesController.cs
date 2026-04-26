using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class MessagesController(
    ILogger<MessagesController> logger,
    IOrderNotificationProducer msgQueue) : ControllerBase
{
    [HttpGet("test")]
    public IActionResult Test1() => Ok("Hello from MessageController");

    [HttpGet("test-msg-queue")]
    public IActionResult TestMessageQueue()
    {
        var notification = new OrderNotification
        {
            UserGuid = Guid.NewGuid(),
            OrderGuid = Guid.NewGuid(),
            Name = "Test User",
            Email = "test@example.com",
            Message = "This is a test message from /test-msg-queue."
        };

        msgQueue.SendMessage(notification);
        logger.LogInformation("Test message sent to queue. UserGuid: {UserGuid}, OrderGuid: {OrderGuid}.",
            notification.UserGuid, notification.OrderGuid);

        return Ok("Test message sent to queue.");
    }

    [HttpPost]
    public IActionResult SendNotification([FromBody] OrderNotification notification)
    {
        msgQueue.SendMessage(notification);
        logger.LogInformation("Notification queued for user {Name} ({Email}), Order {OrderGuid}.",
            notification.Name, notification.Email, notification.OrderGuid);

        return Ok(new { Status = "Notification received" });
    }
}