namespace StockAlert.Application.DTOs;

public record DashboardDto(
    int TotalProducts,
    decimal TotalInventoryValue,
    int LowStockAlerts,
    decimal TotalSalesRevenue,
    int TotalUnitsSold,
    int TotalSuppliers,
    List<TopSellingProductDto> TopSellingProducts,
    List<SaleDto> RecentSales,
    List<StockAdjustmentDto> RecentStockMovements,
    List<LowStockSupplierAlertDto> LowStockSupplierAlerts,
    int DiscrepancyCount,
    List<string> SyncLogs
);
