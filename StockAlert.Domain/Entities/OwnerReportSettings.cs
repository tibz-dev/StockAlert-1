namespace StockAlert.Domain.Entities;

public class OwnerReportSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string RecipientEmail { get; set; }

    public bool DailyEnabled { get; set; } = true;
    public bool WeeklyEnabled { get; set; } = true;
    public bool MonthlyEnabled { get; set; } = true;
    public bool YearlyEnabled { get; set; } = true;

    public int SendHourLocal { get; set; } = 8;
    public int WeeklyDay { get; set; } = (int)DayOfWeek.Monday;
    public int MonthlyDay { get; set; } = 1;
    public string TimeZoneId { get; set; } = "Africa/Johannesburg";

    public DateTime? LastDailySentAt { get; set; }
    public DateTime? LastWeeklySentAt { get; set; }
    public DateTime? LastMonthlySentAt { get; set; }
    public DateTime? LastYearlySentAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}
