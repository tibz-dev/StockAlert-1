using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface ISupplierService
{
    Task<IReadOnlyList<SupplierDto>> GetAllAsync();
    Task<SupplierDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateSupplierRequest request);
    Task<bool> UpdateAsync(Guid id, UpdateSupplierRequest request);
    Task<bool> DeleteAsync(Guid id);
}
