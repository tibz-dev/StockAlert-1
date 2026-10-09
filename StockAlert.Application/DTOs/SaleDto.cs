namespace StockAlert.Application.DTOs;

public record SaleDto(
    Guid Id,
    string? ReceiptNumber,
    string ProductName,
    int Quantity,
    decimal TotalPrice,
    DateTime SaleDate,
    Guid? SalespersonId,
    string? SalespersonName
);
