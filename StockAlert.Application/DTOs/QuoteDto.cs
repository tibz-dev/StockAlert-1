namespace StockAlert.Application.DTOs;

public record QuoteDto(
    Guid Id,
    string QuoteNumber,
    CustomerDto Customer,
    string Status,
    DateTime CreatedAt,
    DateTime ValidUntil,
    DateTime? SentAt,
    DateTime? AcceptedAt,
    DateTime? ConvertedAt,
    string? Notes,
    decimal Subtotal,
    decimal VatRate,
    decimal VatAmount,
    decimal Total,
    string? CreatedBy,
    List<QuoteItemDto> Items
);
