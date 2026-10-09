namespace StockAlert.Domain.Entities;

public class StaffMember
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = "Sales";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
