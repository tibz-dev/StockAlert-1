namespace StockAlert.Application.DTOs;

public record CreateProductRequest(
    string Name,
    string? Description,
    string? Barcode,
    decimal Price,
    int StockQuantity,
    string CategoryName,
    string SupplierName,
    string? SupplierEmail = null
);
