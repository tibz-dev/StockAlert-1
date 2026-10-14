namespace StockAlert.Application.DTOs;

public record StockSourceItemDto(
    string? ExternalProductId,
    string ProductName,
    string? Barcode,
    int QuantityOnHand,
    decimal UnitValue
);
