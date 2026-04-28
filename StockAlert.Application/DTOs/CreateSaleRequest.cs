namespace StockAlert.Application.DTOs;

public record CreateSaleRequest(
    Guid ProductId,
    int Quantity);