using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            return Ok(await _reportingService.GetSummaryAsync(
                fromDate,
                toDate));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("sales/csv")]
    public async Task<IActionResult> DownloadSalesCsv(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var bytes = await _reportingService.GenerateSalesCsvAsync(
                fromDate,
                toDate);

            return File(
                bytes,
                "text/csv",
                $"SalesReport_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stock-movements/csv")]
    public async Task<IActionResult> DownloadStockMovementsCsv(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var bytes =
                await _reportingService.GenerateStockMovementsCsvAsync(
                    fromDate,
                    toDate);

            return File(
                bytes,
                "text/csv",
                $"StockMovements_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("suppliers/csv")]
    public async Task<IActionResult> DownloadSuppliersCsv()
    {
        var bytes = await _reportingService.GenerateSuppliersCsvAsync();

        return File(
            bytes,
            "text/csv",
            $"SuppliersReport_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
