namespace StockAlert.Desktop.Wpf.Models;

public sealed class StaffSession
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string[] Roles { get; set; } = Array.Empty<string>();
}
