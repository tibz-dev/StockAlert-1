namespace StockAlert.Application.DTOs;

public record ResolveStocktakeItemRequest(
    int FinalQuantity,
    string Reason
);
