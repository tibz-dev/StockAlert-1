namespace StockAlert.Application.DTOs;

public record ReportSummaryDto(
    DateTime? FromDate,
    DateTime? ToDate,
    int TotalProducts,
    decimal TotalInventoryValue,
    int LowStockProducts,
    int TotalSuppliers,
    int SaleCount,
    int UnitsSold,
    decimal SalesRevenue,
    int StockMovementCount,
    int UnitsAdded,
    int UnitsRemoved,
    List<TopSellingProductDto> TopSellingProducts,
    List<SalespersonPerformanceDto> Salespeople
);
