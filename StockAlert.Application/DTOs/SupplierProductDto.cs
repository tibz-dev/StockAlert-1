namespace StockAlert.Application.DTOs;

public record SupplierProductDto(
    Guid Id,
    string Name,
    string? Barcode,
    decimal Price,
    int OnHand,
    int Reserved,
    int Available,
    bool IsLowStock
);
