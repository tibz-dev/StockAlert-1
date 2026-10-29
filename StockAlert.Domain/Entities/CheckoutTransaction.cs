namespace StockAlert.Domain.Entities;

/// <summary>A multi-line POS checkout. Not a replacement for legacy single-item Sale records.</summary>
public class CheckoutTransaction
{
    public Guid Id { get; set; }
    public required string ReceiptNumber { get; set; }
    public Guid ClientOperationId { get; set; }
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Draft";
    public decimal Subtotal { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public ICollection<CheckoutLine> Lines { get; set; } = new List<CheckoutLine>();
    public ICollection<CheckoutTender> Tenders { get; set; } = new List<CheckoutTender>();
}
