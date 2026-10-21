using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;
using StockAlert.Domain.Entities;

namespace StockAlert.API.Controllers;

[Authorize(Roles = "Owner")]
[ApiController]
[Route("api/owner-report-settings")]
public class OwnerReportSettingsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public OwnerReportSettingsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var settings = await _context.OwnerReportSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            var email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? throw new InvalidOperationException(
                    "Owner email is unavailable.");

            settings = new OwnerReportSettings
            {
                Id = Guid.NewGuid(),
                RecipientEmail = email,
                UpdatedBy =
                    User.FindFirstValue(ClaimTypes.Name)
                    ?? email
            };

            _context.OwnerReportSettings.Add(settings);
            await _context.SaveChangesAsync(default);
        }

        return Ok(ToDto(settings));
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        UpdateOwnerReportSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientEmail)
            || !request.RecipientEmail.Contains('@'))
        {
            return BadRequest(new { message = "A valid owner report email is required." });
        }

        if (request.SendHourLocal is < 0 or > 23)
        {
            return BadRequest(new { message = "Send hour must be between 0 and 23." });
        }

        if (request.WeeklyDay is < 0 or > 6)
        {
            return BadRequest(new { message = "Weekly day must be between 0 and 6." });
        }

        if (request.MonthlyDay is < 1 or > 28)
        {
            return BadRequest(new { message = "Monthly day must be between 1 and 28." });
        }

        if (string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            return BadRequest(new { message = "Time zone is required." });
        }

        var settings = await _context.OwnerReportSettings
            .FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new OwnerReportSettings
            {
                Id = Guid.NewGuid(),
                RecipientEmail = request.RecipientEmail.Trim()
            };

            _context.OwnerReportSettings.Add(settings);
        }

        settings.RecipientEmail = request.RecipientEmail.Trim();
        settings.DailyEnabled = request.DailyEnabled;
        settings.WeeklyEnabled = request.WeeklyEnabled;
        settings.MonthlyEnabled = request.MonthlyEnabled;
        settings.YearlyEnabled = request.YearlyEnabled;
        settings.SendHourLocal = request.SendHourLocal;
        settings.WeeklyDay = request.WeeklyDay;
        settings.MonthlyDay = request.MonthlyDay;
        settings.TimeZoneId = request.TimeZoneId.Trim();
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy =
            User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email)
            ?? "Owner";

        await _context.SaveChangesAsync(default);

        return Ok(ToDto(settings));
    }

    private static OwnerReportSettingsDto ToDto(
        OwnerReportSettings settings)
    {
        return new OwnerReportSettingsDto(
            settings.RecipientEmail,
            settings.DailyEnabled,
            settings.WeeklyEnabled,
            settings.MonthlyEnabled,
            settings.YearlyEnabled,
            settings.SendHourLocal,
            settings.WeeklyDay,
            settings.MonthlyDay,
            settings.TimeZoneId,
            settings.LastDailySentAt,
            settings.LastWeeklySentAt,
            settings.LastMonthlySentAt,
            settings.LastYearlySentAt
        );
    }
}
