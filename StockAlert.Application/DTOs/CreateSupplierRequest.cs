namespace StockAlert.Application.DTOs;

public record CreateSupplierRequest(
    string CompanyName,
    string? ContactEmail = null
);
