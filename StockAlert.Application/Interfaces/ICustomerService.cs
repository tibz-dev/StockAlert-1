using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerSummaryDto>> GetAllAsync();
    Task<CustomerSummaryDto?> GetByIdAsync(Guid id);
}
