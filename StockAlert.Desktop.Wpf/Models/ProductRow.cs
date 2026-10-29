namespace StockAlert.Desktop.Wpf.Models;

public sealed class ProductRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Barcode { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string CategoryName { get; set; } = "";
    public string SupplierName { get; set; } = "";
}
