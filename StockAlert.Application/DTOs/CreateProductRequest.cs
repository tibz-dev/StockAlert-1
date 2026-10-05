namespace StockAlert.Application.DTOs;

public record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string CategoryName,
    string SupplierName,
    string? SupplierEmail = null
);
