namespace StockAlert.Application.DTOs;

public record StaffMemberDto(
    Guid Id,
    string FullName,
    string? Email,
    string? PhoneNumber,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);
