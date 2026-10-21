namespace StockAlert.Application.DTOs;

public record CreateQuoteRequest(
    Guid? CustomerId,
    string CustomerName,
    string? CustomerCompanyName,
    string? CustomerEmail,
    string? CustomerPhoneNumber,
    string? CustomerWhatsAppNumber,
    bool CustomerHasWhatsApp,
    string? CustomerAddress,
    Guid? SalespersonId,
    DateTime? ValidUntil,
    string? Notes,
    decimal? DepositPercentage,
    decimal? DepositRequired,
    List<CreateQuoteItemRequest> Items
);
