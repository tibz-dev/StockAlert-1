namespace StockAlert.Domain.Entities;

public class Sale
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; }
}