namespace StockAlert.Application.DTOs;

public record AdjustStockRequest(
    int QuantityChange,
    string Reason
);
