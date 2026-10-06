using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IDocumentDeliveryService
{
    Task<PreparedDeliveryDto> PrepareReceiptAsync(
        Guid saleId,
        PrepareDeliveryRequest request);

    Task<PreparedDeliveryDto> PrepareQuoteAsync(
        Guid quoteId,
        PrepareDeliveryRequest request);
}
