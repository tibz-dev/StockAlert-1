namespace StockAlert.Application.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string CategoryName,
    bool IsLowStock,
    string SupplierName,
    string? SupplierEmail,
    string? ExternalId
);
