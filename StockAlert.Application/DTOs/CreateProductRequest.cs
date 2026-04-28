namespace StockAlert.Application.DTOs;

public record CreateProductRequest(
    string Name,
    decimal Price,
    int StockQuantity,
    string CategoryName, 
    string SupplierName  
);