namespace StockAlert.Application.DTOs;

public record SupplierDto(
    Guid Id,
    string CompanyName,
    string? ContactEmail,
    int ProductCount,
    int LowStockProductCount
);
