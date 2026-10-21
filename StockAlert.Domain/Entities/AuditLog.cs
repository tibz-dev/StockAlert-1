namespace StockAlert.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public required string EntityName { get; set; }
    public string? EntityId { get; set; }
    public required string Action { get; set; }
    public required string UserId { get; set; }
    public string? Summary { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Changes { get; set; }
}
