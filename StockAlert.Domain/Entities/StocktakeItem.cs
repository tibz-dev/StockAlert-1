using StockAlert.Domain.Enums;

namespace StockAlert.Domain.Entities;

public class StocktakeItem
{
    public Guid Id { get; set; }

    public Guid StocktakeSessionId { get; set; }
    public StocktakeSession? StocktakeSession { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public string? ExternalProductId { get; set; }
    public required string ProductName { get; set; }
    public string? Barcode { get; set; }
    public decimal UnitValue { get; set; }
    public int ExpectedQuantity { get; set; }

    public int? FirstCount { get; set; }
    public string? FirstCountedBy { get; set; }
    public DateTime? FirstCountedAt { get; set; }

    public int? SecondCount { get; set; }
    public string? SecondCountedBy { get; set; }
    public DateTime? SecondCountedAt { get; set; }

    public int? FinalCount { get; set; }
    public StocktakeItemStatus Status { get; set; } =
        StocktakeItemStatus.Pending;

    public string? ResolutionReason { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
