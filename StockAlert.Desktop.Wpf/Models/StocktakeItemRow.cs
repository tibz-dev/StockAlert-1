namespace StockAlert.Desktop.Wpf.Models;
public sealed class StocktakeItemRow
{
 public Guid Id {get;set;}
 public string ProductName {get;set;} = "";
 public string? Barcode {get;set;}
 public int? ExpectedQuantity {get;set;}
 public int? FirstCount {get;set;}
 public int? SecondCount {get;set;}
 public string Status {get;set;} = "";
}
