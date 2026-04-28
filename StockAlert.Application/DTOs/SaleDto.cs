namespace StockAlert.Application.DTOs;

public record SaleDto(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal TotalPrice,
    DateTime SaleDate
);