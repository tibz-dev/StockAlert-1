namespace StockAlert.Domain.Entities;

public class StockAdjustment
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int PreviousQuantity { get; set; }
    public int QuantityChange { get; set; }
    public int NewQuantity { get; set; }

    public required string Reason { get; set; }
    public required string PerformedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
