namespace StockAlert.Domain.Entities;

public class Product
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string? ExternalId { get; set; } // The ID from SmartTrade
    public string? ExternalSource { get; set; } // e.g., "SmartTrade"

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();
    public ICollection<QuoteItem> QuoteItems { get; set; } = new List<QuoteItem>();
}