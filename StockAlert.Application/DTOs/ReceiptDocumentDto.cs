namespace StockAlert.Application.DTOs;

public record ReceiptDocumentDto(
    string ReceiptNumber,
    DateTime SaleDate,
    CustomerDto Customer,
    BusinessProfileDto Business,
    Guid? SalespersonId,
    string? SalespersonName,
    decimal Total,
    List<ReceiptLineDto> Lines
);
