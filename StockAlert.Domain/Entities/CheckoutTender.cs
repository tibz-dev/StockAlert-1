namespace StockAlert.Domain.Entities;
public class CheckoutTender
{
    public Guid Id { get; set; }
    public Guid CheckoutTransactionId { get; set; }
    public CheckoutTransaction CheckoutTransaction { get; set; } = null!;
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ExternalReference { get; set; }
    public string Status { get; set; } = "Pending";
}