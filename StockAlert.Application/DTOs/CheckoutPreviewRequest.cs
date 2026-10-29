namespace StockAlert.Application.DTOs;

public record CheckoutPreviewItemRequest(Guid ProductId, int Quantity);

public record CheckoutPreviewRequest(List<CheckoutPreviewItemRequest> Items);

public record CheckoutPreviewLineDto(
    Guid ProductId,
    string ProductName,
    string? Barcode,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    int AvailableQuantity);

public record CheckoutPreviewDto(
    List<CheckoutPreviewLineDto> Items,
    decimal Subtotal,
    long TotalUnits,
    DateTime PricedAtUtc,
    string Note);
