using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IBarcodeLookupService
{
    Task<BarcodeLookupResultDto> LookupAsync(string barcode);
}
