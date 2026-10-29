using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Infrastructure.Services;

namespace StockAlert.API.Controllers;

[ApiController]
[Route("api/staff-badges")]
public class StaffBadgesController : ControllerBase
{
    private readonly StaffBadgeService _badges;
    public StaffBadgesController(StaffBadgeService badges) => _badges = badges;

    public record EnrolBadgeRequest(string UserId);
    public record BadgeLoginRequest(string Badge, string Password);

    [Authorize(Roles = "Owner,Manager")]
    [HttpPost("enrol")]
    public async Task<IActionResult> Enrol(EnrolBadgeRequest request, CancellationToken ct)
    {
        try
        {
            var token = await _badges.EnrolAsync(request.UserId, ct);
            Response.Headers.CacheControl = "no-store";
            return Ok(new { badge = token, message = "Print securely. The badge credential is displayed only once." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize(Roles = "Owner,Manager")]
    [HttpPost("{id:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct) =>
        await _badges.RevokeAsync(id, ct) ? NoContent() : NotFound();

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(BadgeLoginRequest request, CancellationToken ct)
    {
        var result = await _badges.LoginAsync(request.Badge, request.Password, ct);
        Response.Headers.CacheControl = "no-store";
        return result.Success ? Ok(result) : Unauthorized(new AuthResponse(false, "", "Invalid credentials."));
    }
}
