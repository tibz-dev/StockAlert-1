using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize(Roles = "Owner,Manager")]
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AuditController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entity = null,
        [FromQuery] string? user = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1)
        {
            page = 1;
        }

        pageSize = Math.Clamp(pageSize, 10, 200);

        var query = BuildQuery(
            fromDate,
            toDate,
            action,
            entity,
            user,
            search);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(log => log.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            items = logs,
            totalCount,
            page,
            pageSize,
            totalPages = Math.Max(
                1,
                (int)Math.Ceiling(totalCount / (double)pageSize))
        });
    }

    [HttpGet("csv")]
    public async Task<IActionResult> DownloadCsv(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entity = null,
        [FromQuery] string? user = null,
        [FromQuery] string? search = null)
    {
        var logs = await BuildQuery(
                fromDate,
                toDate,
                action,
                entity,
                user,
                search)
            .OrderByDescending(log => log.Timestamp)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine(
            "Timestamp,User,IP Address,Entity,Entity ID,Action,Summary,Details");

        foreach (var log in logs)
        {
            builder.AppendLine(string.Join(",",
                Csv(log.Timestamp.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture)),
                Csv(log.UserId),
                Csv(log.IpAddress ?? string.Empty),
                Csv(log.EntityName),
                Csv(log.EntityId ?? string.Empty),
                Csv(log.Action),
                Csv(log.Summary ?? string.Empty),
                Csv(log.Changes ?? string.Empty)));
        }

        return File(
            Encoding.UTF8.GetBytes(builder.ToString()),
            "text/csv",
            $"AuditTrail_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private IQueryable<StockAlert.Domain.Entities.AuditLog> BuildQuery(
        DateTime? fromDate,
        DateTime? toDate,
        string? action,
        string? entity,
        string? user,
        string? search)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(log => log.Timestamp >= from);
        }

        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(log => log.Timestamp < toExclusive);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var value = action.Trim();
            query = query.Where(log => log.Action == value);
        }

        if (!string.IsNullOrWhiteSpace(entity))
        {
            var value = entity.Trim();
            query = query.Where(log => log.EntityName == value);
        }

        if (!string.IsNullOrWhiteSpace(user))
        {
            var value = user.Trim();
            query = query.Where(log => log.UserId.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();

            query = query.Where(log =>
                log.EntityName.Contains(value)
                || log.Action.Contains(value)
                || log.UserId.Contains(value)
                || (log.Summary != null && log.Summary.Contains(value))
                || (log.Changes != null && log.Changes.Contains(value)));
        }

        return query;
    }

    private static string Csv(string value)
    {
        var safe = value;

        if (safe.StartsWith("=")
            || safe.StartsWith("+")
            || safe.StartsWith("-")
            || safe.StartsWith("@"))
        {
            safe = "'" + safe;
        }

        if (!safe.Contains(',')
            && !safe.Contains('"')
            && !safe.Contains('\n')
            && !safe.Contains('\r'))
        {
            return safe;
        }

        return $"\"{safe.Replace("\"", "\"\"")}\"";
    }
}
