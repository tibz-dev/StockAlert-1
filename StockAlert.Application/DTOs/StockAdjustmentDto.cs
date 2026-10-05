namespace StockAlert.Application.DTOs;

public record StockAdjustmentDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int PreviousQuantity,
    int QuantityChange,
    int NewQuantity,
    string Reason,
    string PerformedBy,
    DateTime CreatedAt
);
