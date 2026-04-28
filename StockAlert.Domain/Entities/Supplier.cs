namespace StockAlert.Domain.Entities;


public class Supplier
{
    public Guid Id { get; set; }
    public required string CompanyName { get; set; }
    public string? ContactEmail { get; set; }
}