namespace StockAlert.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity,
    string CategoryName,
    bool IsLowStock,
    string SupplierName,
    string? SupplierEmail,
    string? ExternalId
);
