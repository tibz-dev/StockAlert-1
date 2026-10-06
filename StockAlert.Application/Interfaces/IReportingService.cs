using StockAlert.Application.DTOs;

namespace StockAlert.Application.Interfaces;

public interface IReportingService
{
    Task<ReportSummaryDto> GetSummaryAsync(
        DateTime? fromDate,
        DateTime? toDate);

    Task<byte[]> GenerateSalesCsvAsync(
        DateTime? fromDate,
        DateTime? toDate);

    Task<byte[]> GenerateStockMovementsCsvAsync(
        DateTime? fromDate,
        DateTime? toDate);

    Task<byte[]> GenerateSuppliersCsvAsync();
}
