namespace StockAlert.Desktop.Wpf.Models;
public sealed class StocktakeSessionRow
{
 public Guid Id {get;set;}
 public string ReferenceNumber {get;set;} = "";
 public string Title {get;set;} = "";
 public string Status {get;set;} = "";
 public string? Location {get;set;}
 public int TotalItems {get;set;}
 public int CountedItems {get;set;}
 public int VarianceItems {get;set;}
 public DateTime CreatedAt {get;set;}
}
