namespace StockAlert.Application.DTOs;

public record CreateStaffMemberRequest(
    string FullName,
    string? Email,
    string? PhoneNumber,
    string Role
);
