namespace StockAlert.Application.DTOs;

public record QuoteItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int CurrentStock,
    int ReservedQuantity,
    int AvailableQuantity
);
