namespace StockAlert.Domain.Entities;

public class Sale
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? ReceiptNumber { get; set; }

    public Guid? SalespersonId { get; set; }
    public StaffMember? Salesperson { get; set; }
    public string? SalespersonName { get; set; }

    public Guid? ClientOperationId { get; set; }
    public string? DeviceId { get; set; }
    public DateTime? ClientCreatedAt { get; set; }
    public bool WasQueuedOffline { get; set; }

    public int Quantity { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; }
}
