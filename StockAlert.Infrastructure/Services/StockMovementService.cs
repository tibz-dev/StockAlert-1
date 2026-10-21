using Microsoft.EntityFrameworkCore;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.Infrastructure.Services;

public class StockMovementService : IStockMovementService
{
    private readonly IApplicationDbContext _context;

    public StockMovementService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<StockAdjustmentDto>> GetAsync(
        Guid? productId = null,
        int take = 100)
    {
        var safeTake = Math.Clamp(take, 1, 500);

        var query = _context.StockAdjustments
            .AsNoTracking()
            .Include(adjustment => adjustment.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(adjustment =>
                adjustment.ProductId == productId.Value);
        }

        return await query
            .OrderByDescending(adjustment => adjustment.CreatedAt)
            .Take(safeTake)
            .Select(adjustment => new StockAdjustmentDto(
                adjustment.Id,
                adjustment.ProductId,
                adjustment.Product != null
                    ? adjustment.Product.Name
                    : "Unknown Product",
                adjustment.PreviousQuantity,
                adjustment.QuantityChange,
                adjustment.NewQuantity,
                adjustment.Reason,
                adjustment.PerformedBy,
                adjustment.CreatedAt
            ))
            .ToListAsync();
    }
}
