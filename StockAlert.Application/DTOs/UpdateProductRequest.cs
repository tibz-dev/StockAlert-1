namespace StockAlert.Application.DTOs;

public record UpdateProductRequest(
    string Name,
    string? Description,
    string? Barcode,
    decimal Price,
    string CategoryName,
    string SupplierName,
    string? SupplierEmail = null
);
