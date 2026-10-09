namespace StockAlert.Domain.Entities;

public class QuotePayment
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public Quote? Quote { get; set; }

    public decimal Amount { get; set; }
    public required string Method { get; set; }
    public string? Reference { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? RecordedBy { get; set; }
}
