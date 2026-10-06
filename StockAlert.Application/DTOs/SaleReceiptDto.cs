namespace StockAlert.Application.DTOs;

public record SaleReceiptDto(
    Guid SaleId,
    string ReceiptNumber,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    DateTime SaleDate,
    CustomerDto? Customer
);
