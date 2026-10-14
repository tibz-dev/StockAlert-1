namespace StockAlert.Application.DTOs;

public record CreateStocktakeRequest(
    string Title,
    string? Location,
    string? Zone,
    string Source = "StockAlert",
    bool BlindCount = true
);
