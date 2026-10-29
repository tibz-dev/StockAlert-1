namespace StockAlert.Desktop.Wpf.Models;
public sealed class StocktakeDetail
{
 public Guid Id {get;set;}
 public string Title {get;set;} = "";
 public string Status {get;set;} = "";
 public List<StocktakeItemRow> Items {get;set;} = new();
}
