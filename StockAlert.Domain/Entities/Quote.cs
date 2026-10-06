using StockAlert.Domain.Enums;

namespace StockAlert.Domain.Entities;

public class Quote
{
    public Guid Id { get; set; }
    public required string QuoteNumber { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ConvertedAt { get; set; }

    public string? Notes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public string? CreatedBy { get; set; }

    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
