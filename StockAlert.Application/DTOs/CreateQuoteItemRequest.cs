namespace StockAlert.Application.DTOs;

public record CreateQuoteItemRequest(
    Guid ProductId,
    int Quantity,
    decimal? UnitPrice = null
);
