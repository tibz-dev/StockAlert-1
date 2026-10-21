using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<Guid> CreateProductAsync(CreateProductRequest request);
    Task<bool> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(Guid id, string performedBy);
    Task<bool> AdjustStockAsync(Guid id, AdjustStockRequest request, string performedBy);

    Task<SaleReceiptDto?> RecordSaleAsync(CreateSaleRequest request, string salespersonFallback);
    Task<IEnumerable<SaleDto>> GetAllSalesAsync();

    Task<DashboardDto> GetDashboardStatsAsync();
    Task<int> SyncWithSmartTradeAsync();
    Task<byte[]> GenerateStockReportAsync();
}
