namespace StockAlert.Domain.Entities;

public class CheckoutLine
{
    public Guid Id { get; set; }
    public Guid CheckoutTransactionId { get; set; }
    public CheckoutTransaction CheckoutTransaction { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
