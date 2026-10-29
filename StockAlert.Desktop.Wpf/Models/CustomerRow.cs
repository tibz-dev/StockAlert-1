namespace StockAlert.Desktop.Wpf.Models;

public sealed class CustomerRow
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WhatsAppNumber { get; set; }
    public bool HasWhatsApp { get; set; }
    public string? Address { get; set; }
}
