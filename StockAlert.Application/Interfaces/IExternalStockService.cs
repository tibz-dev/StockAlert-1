using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IExternalStockService
{
    // Pull stock levels from SmartTrade
    Task<IEnumerable<ProductDto>> SyncFromExternalAsync();

    // Push a sale made in StockAlert to SmartTrade
    Task<bool> PushSaleToExternalAsync(SaleDto sale);
}