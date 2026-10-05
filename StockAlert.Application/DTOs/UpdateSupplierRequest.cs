namespace StockAlert.Application.DTOs;

public record UpdateSupplierRequest(
    string CompanyName,
    string? ContactEmail = null
);
