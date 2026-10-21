using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IStocktakeService
{
    Task<IReadOnlyList<StocktakeSessionDto>> GetAllAsync(
        bool canSeeExpected);

    Task<StocktakeSessionDto?> GetByIdAsync(
        Guid id,
        bool canSeeExpected);

    Task<Guid> CreateAsync(
        CreateStocktakeRequest request,
        string createdBy);

    Task<StocktakeItemDto?> FindByBarcodeAsync(
        Guid sessionId,
        string barcode,
        bool canSeeExpected);

    Task<StocktakeItemDto?> SubmitCountAsync(
        Guid sessionId,
        Guid itemId,
        SubmitStocktakeCountRequest request,
        string performedBy,
        bool canSeeExpected);

    Task<StocktakeItemDto?> SubmitRecountAsync(
        Guid sessionId,
        Guid itemId,
        SubmitStocktakeCountRequest request,
        string performedBy,
        bool canSeeExpected);

    Task<StocktakeItemDto?> ResolveAsync(
        Guid sessionId,
        Guid itemId,
        ResolveStocktakeItemRequest request,
        string approvedBy);

    Task<bool> CompleteAsync(
        Guid sessionId,
        string completedBy);
}
