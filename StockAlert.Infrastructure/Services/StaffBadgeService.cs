using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Domain.Entities;
using StockAlert.Infrastructure.Persistence;

namespace StockAlert.Infrastructure.Services;

public sealed class StaffBadgeService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AuthService _auth;

    public StaffBadgeService(ApplicationDbContext db,
        UserManager<ApplicationUser> users, AuthService auth)
    {
        _db = db;
        _users = users;
        _auth = auth;
    }

    private static string Hash(string badge) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(badge)));

    // The raw credential is shown once, not stored in the database.
    public async Task<string> EnrolAsync(string userId, CancellationToken ct)
    {
        if (await _users.FindByIdAsync(userId) is null)
            throw new ArgumentException("Staff account not found.");
        var secret = "SA-STAFF-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.StaffBadges.Add(new StaffBadge
        {
            Id = Guid.NewGuid(), UserId = userId, BadgeHash = Hash(secret)
        });
        await _db.SaveChangesAsync(ct);
        return secret;
    }

    public async Task<bool> RevokeAsync(Guid id, CancellationToken ct)
    {
        var badge = await _db.StaffBadges.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (badge is null) return false;
        badge.IsRevoked = true;
        badge.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AuthResponse> LoginAsync(string badge, string password, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(badge) ||
            badge.Length > 100)
            return new AuthResponse(false, "", "Invalid credentials.");

        var record = await _db.StaffBadges.AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.BadgeHash == Hash(badge.Trim())
                && !x.IsRevoked, ct);
        if (record?.User?.Email is null)
            return new AuthResponse(false, "", "Invalid credentials.");

        // A scanned badge is an identifier, NEVER sufficient alone to sign in.
        return await _auth.LoginAsync(new LoginRequest(record.User.Email, password));
    }
}
