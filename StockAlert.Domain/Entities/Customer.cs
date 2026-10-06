namespace StockAlert.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WhatsAppNumber { get; set; }
    public bool HasWhatsApp { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
