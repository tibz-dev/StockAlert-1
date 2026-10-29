namespace StockAlert.Application.DTOs;

public record CommitCheckoutRequest(
    Guid ClientOperationId,
    List<CheckoutPreviewItemRequest> Items,
    bool CashReceived,
    decimal ExpectedTotal,
    Guid? CustomerId = null);

public record CheckoutReceiptDto(
    Guid Id,
    string ReceiptNumber,
    string Status,
    decimal Subtotal,
    decimal VatAmount,
    decimal Total,
    DateTime CreatedAtUtc,
    List<CheckoutPreviewLineDto> Items);
