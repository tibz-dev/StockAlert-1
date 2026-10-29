namespace StockAlert.Domain.Entities;

public class StaffBadge
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string BadgeHash { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
}
