namespace StockAlert.Desktop.Wpf.Models;

public sealed class SupplierRow
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = "";
    public string? ContactEmail { get; set; }
    public int ProductCount { get; set; }
    public int LowStockProductCount { get; set; }
}
