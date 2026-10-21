namespace StockAlert.Application.DTOs;

public record SalespersonPerformanceDto(
    string SalespersonName,
    int SaleCount,
    int UnitsSold,
    decimal Revenue
);
