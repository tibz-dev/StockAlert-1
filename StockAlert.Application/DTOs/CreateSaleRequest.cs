namespace StockAlert.Application.DTOs;

public record CreateSaleRequest(
    Guid ProductId,
    int Quantity,
    string? CustomerName = null,
    string? CustomerCompanyName = null,
    string? CustomerEmail = null,
    string? CustomerPhoneNumber = null,
    string? CustomerWhatsAppNumber = null,
    bool CustomerHasWhatsApp = false,
    string? CustomerAddress = null
);
