namespace StockAlert.Application.DTOs;

public record QuotePaymentDto(
    Guid Id,
    decimal Amount,
    string Method,
    string? Reference,
    DateTime PaidAt,
    string? RecordedBy
);
