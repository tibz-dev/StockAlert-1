namespace StockAlert.Desktop.Wpf.Models;

public sealed class SaleReceiptResult
{
    public Guid SaleId { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime SaleDate { get; set; }
}
