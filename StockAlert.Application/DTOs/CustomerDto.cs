namespace StockAlert.Application.DTOs;

public record CustomerDto(
    Guid Id,
    string FullName,
    string? CompanyName,
    string? Email,
    string? PhoneNumber,
    string? WhatsAppNumber,
    bool HasWhatsApp,
    string? Address
);
