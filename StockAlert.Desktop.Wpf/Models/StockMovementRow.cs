namespace StockAlert.Desktop.Wpf.Models;
public sealed class StockMovementRow
{
 public Guid Id {get;set;}
 public string ProductName {get;set;} = "";
 public int PreviousQuantity {get;set;}
 public int QuantityChange {get;set;}
 public int NewQuantity {get;set;}
 public string Reason {get;set;} = "";
 public string PerformedBy {get;set;} = "";
 public DateTime CreatedAt {get;set;}
}
