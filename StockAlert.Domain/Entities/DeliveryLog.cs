using StockAlert.Domain.Enums;

namespace StockAlert.Domain.Entities;

public class DeliveryLog
{
    public Guid Id { get; set; }
    public required string DocumentType { get; set; }
    public Guid DocumentId { get; set; }
    public DeliveryChannel Channel { get; set; }
    public required string Destination { get; set; }
    public DeliveryStatus Status { get; set; }
    public string? ActionUrl { get; set; }
    public string? ProviderReference { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
