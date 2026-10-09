namespace StockAlert.Application.DTOs;

public record UpdateOwnerReportSettingsRequest(
    string RecipientEmail,
    bool DailyEnabled,
    bool WeeklyEnabled,
    bool MonthlyEnabled,
    bool YearlyEnabled,
    int SendHourLocal,
    int WeeklyDay,
    int MonthlyDay,
    string TimeZoneId
);
