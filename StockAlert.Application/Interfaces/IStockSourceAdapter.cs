using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IStockSourceAdapter
{
    string SourceName { get; }

    Task<IReadOnlyList<StockSourceItemDto>> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}
