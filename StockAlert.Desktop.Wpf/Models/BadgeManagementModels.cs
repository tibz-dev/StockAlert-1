namespace StockAlert.Desktop.Wpf.Models;

public sealed class BadgeStaffRow
{
    public string UserId { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string Display => $"{FullName} ({Email})";
}

public sealed class BadgeAssignmentRow
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string StaffName { get; set; } = "";
    public string? StaffEmail { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

public sealed class EnrolledBadgeResult
{
    public string Badge { get; set; } = "";
    public string Message { get; set; } = "";
}
