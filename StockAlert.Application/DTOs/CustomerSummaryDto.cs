namespace StockAlert.Application.DTOs;

public record CustomerSummaryDto(
    Guid Id,
    string FullName,
    string? CompanyName,
    string? Email,
    string? PhoneNumber,
    string? WhatsAppNumber,
    bool HasWhatsApp,
    string? Address,
    int QuoteCount,
    int SaleCount,
    decimal LifetimeValue,
    DateTime? LastActivityAt
);
