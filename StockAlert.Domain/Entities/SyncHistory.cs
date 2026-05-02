namespace StockAlert.Domain.Entities;

public class SyncHistory
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ExternalSource { get; set; } = "SmartTrade";
    public int ItemsProcessed { get; set; }
    public int DiscrepanciesFound { get; set; }
    public string Status { get; set; } = "Success"; // or "Failed"
}