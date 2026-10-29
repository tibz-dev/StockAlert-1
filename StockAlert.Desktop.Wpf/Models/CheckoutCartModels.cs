namespace StockAlert.Desktop.Wpf.Models;

public sealed class CheckoutCartLine
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public sealed class CheckoutPreview
{
    public decimal Subtotal { get; set; }
    public int TotalUnits { get; set; }
    public string Note { get; set; } = "";
    public List<CheckoutCartLine> Items { get; set; } = new();
}

public sealed class CheckoutReceipt
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
}
