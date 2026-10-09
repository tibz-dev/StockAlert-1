using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.Infrastructure.BackgroundServices;

public class OwnerReportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OwnerReportWorker> _logger;

    public OwnerReportWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OwnerReportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

        await RunOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<IApplicationDbContext>();
            var reporting = scope.ServiceProvider
                .GetRequiredService<IReportingService>();
            var sender = scope.ServiceProvider
                .GetRequiredService<IOutboundMessageSender>();

            var settings = await context.OwnerReportSettings
                .FirstOrDefaultAsync(cancellationToken);

            if (settings == null)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var timeZone = ResolveTimeZone(settings.TimeZoneId);
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
                nowUtc,
                timeZone);

            if (nowLocal.Hour < settings.SendHourLocal)
            {
                return;
            }

            var dailyScheduledDate = nowLocal.Date;

            if (settings.DailyEnabled
                && IsDue(
                    settings.LastDailySentAt,
                    dailyScheduledDate,
                    timeZone))
            {
                var end = dailyScheduledDate.AddDays(-1);

                await SendReportAsync(
                    "Daily",
                    end,
                    end,
                    settings,
                    reporting,
                    context,
                    sender,
                    cancellationToken);

                settings.LastDailySentAt = nowUtc;
                await context.SaveChangesAsync(cancellationToken);
            }

            var weeklyScheduledDate = GetLatestWeeklyScheduleDate(
                nowLocal.Date,
                settings.WeeklyDay);

            if (settings.WeeklyEnabled
                && IsDue(
                    settings.LastWeeklySentAt,
                    weeklyScheduledDate,
                    timeZone))
            {
                var end = weeklyScheduledDate.AddDays(-1);
                var start = end.AddDays(-6);

                await SendReportAsync(
                    "Weekly",
                    start,
                    end,
                    settings,
                    reporting,
                    context,
                    sender,
                    cancellationToken);

                settings.LastWeeklySentAt = nowUtc;
                await context.SaveChangesAsync(cancellationToken);
            }

            var monthlyScheduledDate =
                GetLatestMonthlyScheduleDate(
                    nowLocal.Date,
                    settings.MonthlyDay);

            if (settings.MonthlyEnabled
                && IsDue(
                    settings.LastMonthlySentAt,
                    monthlyScheduledDate,
                    timeZone))
            {
                var firstOfScheduleMonth = new DateTime(
                    monthlyScheduledDate.Year,
                    monthlyScheduledDate.Month,
                    1);

                var end = firstOfScheduleMonth.AddDays(-1);
                var start = new DateTime(
                    end.Year,
                    end.Month,
                    1);

                await SendReportAsync(
                    "Monthly",
                    start,
                    end,
                    settings,
                    reporting,
                    context,
                    sender,
                    cancellationToken);

                settings.LastMonthlySentAt = nowUtc;
                await context.SaveChangesAsync(cancellationToken);
            }

            var yearlyScheduledDate = new DateTime(
                nowLocal.Year,
                1,
                1);

            if (settings.YearlyEnabled
                && IsDue(
                    settings.LastYearlySentAt,
                    yearlyScheduledDate,
                    timeZone))
            {
                var year = yearlyScheduledDate.Year - 1;
                var start = new DateTime(year, 1, 1);
                var end = new DateTime(year, 12, 31);

                await SendReportAsync(
                    "Yearly",
                    start,
                    end,
                    settings,
                    reporting,
                    context,
                    sender,
                    cancellationToken);

                settings.LastYearlySentAt = nowUtc;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Owner report worker failed.");
        }
    }

    private static async Task SendReportAsync(
        string cadence,
        DateTime fromDate,
        DateTime toDate,
        OwnerReportSettings settings,
        IReportingService reporting,
        IApplicationDbContext context,
        IOutboundMessageSender sender,
        CancellationToken cancellationToken)
    {
        var summary = await reporting.GetSummaryAsync(
            fromDate,
            toDate);

        var toExclusive = toDate.Date.AddDays(1);

        var auditLogs = await context.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.Timestamp >= fromDate.Date
                && log.Timestamp < toExclusive)
            .OrderByDescending(log => log.Timestamp)
            .Take(20)
            .ToListAsync(cancellationToken);

        var auditCount = await context.AuditLogs
            .AsNoTracking()
            .CountAsync(
                log =>
                    log.Timestamp >= fromDate.Date
                    && log.Timestamp < toExclusive,
                cancellationToken);

        var body = BuildBody(
            cadence,
            fromDate,
            toDate,
            summary.SalesRevenue,
            summary.SaleCount,
            summary.UnitsSold,
            summary.TotalInventoryValue,
            summary.LowStockProducts,
            auditCount,
            auditLogs);

        var result = await sender.SendEmailAsync(
            settings.RecipientEmail,
            $"StockAlert {cadence} Owner Report - {toDate:yyyy-MM-dd}",
            body);

        if (!result.Sent)
        {
            throw new InvalidOperationException(
                result.ErrorMessage
                ?? "Owner report email could not be sent.");
        }
    }

    private static string BuildBody(
        string cadence,
        DateTime fromDate,
        DateTime toDate,
        decimal salesRevenue,
        int saleCount,
        int unitsSold,
        decimal inventoryValue,
        int lowStockProducts,
        int auditCount,
        IReadOnlyList<AuditLog> logs)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"StockAlert {cadence} Owner Report");
        builder.AppendLine(
            $"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
        builder.AppendLine();

        builder.AppendLine("OPERATIONAL REPORT");
        builder.AppendLine($"Sales revenue: {salesRevenue:0.00}");
        builder.AppendLine($"Sales transactions: {saleCount}");
        builder.AppendLine($"Units sold: {unitsSold}");
        builder.AppendLine($"Current inventory value: {inventoryValue:0.00}");
        builder.AppendLine($"Current low-stock products: {lowStockProducts}");
        builder.AppendLine();

        builder.AppendLine("AUDIT TRAIL");
        builder.AppendLine($"Audit activities in period: {auditCount}");

        foreach (var log in logs)
        {
            builder.AppendLine(
                $"- {log.Timestamp:yyyy-MM-dd HH:mm} | " +
                $"{log.UserId} | {log.Action} | " +
                $"{log.Summary ?? log.EntityName}");
        }

        if (auditCount > logs.Count)
        {
            builder.AppendLine(
                $"... plus {auditCount - logs.Count} additional audit activities.");
        }

        builder.AppendLine();
        builder.AppendLine(
            "This report was generated automatically by StockAlert.");

        return builder.ToString();
    }

    private static bool IsDue(
        DateTime? lastSentUtc,
        DateTime scheduledLocalDate,
        TimeZoneInfo timeZone)
    {
        if (!lastSentUtc.HasValue)
        {
            return true;
        }

        var lastLocal = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(
                lastSentUtc.Value,
                DateTimeKind.Utc),
            timeZone);

        return lastLocal.Date < scheduledLocalDate.Date;
    }

    private static DateTime GetLatestWeeklyScheduleDate(
        DateTime today,
        int weeklyDay)
    {
        var daysSinceScheduled =
            ((int)today.DayOfWeek - weeklyDay + 7) % 7;

        return today.AddDays(-daysSinceScheduled);
    }

    private static DateTime GetLatestMonthlyScheduleDate(
        DateTime today,
        int monthlyDay)
    {
        var safeDay = Math.Clamp(monthlyDay, 1, 28);

        if (today.Day >= safeDay)
        {
            return new DateTime(
                today.Year,
                today.Month,
                safeDay);
        }

        var previousMonth = today.AddMonths(-1);

        return new DateTime(
            previousMonth.Year,
            previousMonth.Month,
            safeDay);
    }

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch
        {
            foreach (var fallback in new[]
            {
                "Africa/Johannesburg",
                "South Africa Standard Time"
            })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(fallback);
                }
                catch
                {
                }
            }

            return TimeZoneInfo.Utc;
        }
    }
}
