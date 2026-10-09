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
    DateTime? ValidUntil,
    string? Notes,
    decimal? DepositRequired,
    List<CreateQuoteItemRequest> Items
);
