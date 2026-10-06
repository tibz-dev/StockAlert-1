namespace StockAlert.Application.DTOs;

public record QuoteConversionDto(
    Guid QuoteId,
    string QuoteNumber,
    Guid PrimarySaleId,
    string ReceiptNumber,
    decimal Total,
    CustomerDto Customer
);
