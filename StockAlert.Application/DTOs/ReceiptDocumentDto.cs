namespace StockAlert.Application.DTOs;

public record ReceiptDocumentDto(
    string ReceiptNumber,
    DateTime SaleDate,
    CustomerDto Customer,
    BusinessProfileDto Business,
    decimal Total,
    List<ReceiptLineDto> Lines
);
