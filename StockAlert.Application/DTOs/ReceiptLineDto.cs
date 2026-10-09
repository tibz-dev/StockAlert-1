namespace StockAlert.Application.DTOs;

public record ReceiptLineDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
