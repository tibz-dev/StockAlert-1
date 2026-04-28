using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<Guid> CreateProductAsync(CreateProductRequest request);
}