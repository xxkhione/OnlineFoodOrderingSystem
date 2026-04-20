public class OrderNotification
{
    public Guid UserGuid { get; set; }
    public Guid OrderGuid { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public required string Message { get; set; }
}