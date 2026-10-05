using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IStockMovementService
{
    Task<IReadOnlyList<StockAdjustmentDto>> GetAsync(
        Guid? productId = null,
        int take = 100);
}
