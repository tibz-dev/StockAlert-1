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
    decimal DepositRequired,
    decimal AmountPaid,
    decimal BalanceDue,
    string PaymentStatus,
    string? CreatedBy,
    List<QuoteItemDto> Items,
    List<QuotePaymentDto> Payments
);
