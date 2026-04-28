namespace StockAlert.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public required string EntityName { get; set; }
    public required string Action { get; set; } 
    public required string UserId { get; set; } // Will be linked to JWT later
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Changes { get; set; } 
}