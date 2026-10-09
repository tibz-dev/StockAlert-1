namespace StockAlert.Application.DTOs;

public record RecordQuotePaymentRequest(
    decimal Amount,
    string Method,
    string? Reference = null,
    DateTime? PaidAt = null
);
