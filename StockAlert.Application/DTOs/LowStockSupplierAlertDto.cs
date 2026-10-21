namespace StockAlert.Application.DTOs;

public record LowStockSupplierAlertDto(
    Guid SupplierId,
    string SupplierName,
    string? SupplierEmail,
    int LowStockProductCount
);
