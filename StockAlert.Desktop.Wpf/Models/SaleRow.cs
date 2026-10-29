namespace StockAlert.Desktop.Wpf.Models;
public sealed class SaleRow
{
 public Guid Id {get;set;}
 public string? ReceiptNumber {get;set;}
 public string ProductName {get;set;} = "";
 public int Quantity {get;set;}
 public decimal TotalPrice {get;set;}
 public DateTime SaleDate {get;set;}
 public string? SalespersonName {get;set;}
}
