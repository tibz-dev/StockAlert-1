namespace StockAlert.Application.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string FullName
);

