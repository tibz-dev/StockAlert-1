namespace StockAlert.Desktop.Wpf.Models;
public sealed class QuoteRow
{
 public Guid Id {get;set;}
 public string QuoteNumber {get;set;} = "";
 public CustomerRow? Customer {get;set;}
 public string CustomerName => Customer?.FullName ?? "";
 public string Status {get;set;} = "";
 public decimal Total {get;set;}
 public decimal AmountPaid {get;set;}
 public decimal BalanceDue {get;set;}
 public DateTime CreatedAt {get;set;}
 public DateTime ValidUntil {get;set;}
}
