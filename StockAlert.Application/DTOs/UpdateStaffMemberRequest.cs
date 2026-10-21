namespace StockAlert.Application.DTOs;

public record UpdateStaffMemberRequest(
    string FullName,
    string? Email,
    string? PhoneNumber,
    string Role,
    bool IsActive
);
