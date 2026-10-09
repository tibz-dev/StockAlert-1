namespace StockAlert.Application.DTOs;

public record OwnerReportSettingsDto(
    string RecipientEmail,
    bool DailyEnabled,
    bool WeeklyEnabled,
    bool MonthlyEnabled,
    bool YearlyEnabled,
    int SendHourLocal,
    int WeeklyDay,
    int MonthlyDay,
    string TimeZoneId,
    DateTime? LastDailySentAt,
    DateTime? LastWeeklySentAt,
    DateTime? LastMonthlySentAt,
    DateTime? LastYearlySentAt
);
