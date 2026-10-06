using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IQuoteService
{
    Task<IReadOnlyList<QuoteDto>> GetAllAsync();
    Task<QuoteDto?> GetByIdAsync(Guid id);
    Task<Guid> CreateAsync(CreateQuoteRequest request, string createdBy);
    Task<QuoteDto?> UpdateStatusAsync(Guid id, UpdateQuoteStatusRequest request);
    Task<QuoteConversionDto?> ConvertToSaleAsync(Guid id);
}
