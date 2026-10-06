namespace StockAlert.Application.DTOs;

public record TopSellingProductDto(
    Guid ProductId,
    string ProductName,
    int UnitsSold,
    decimal Revenue,
    int StockQuantity
);
