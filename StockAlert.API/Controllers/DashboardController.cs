using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IProductService _productService;

    public DashboardController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var stats = await _productService.GetDashboardStatsAsync();
        return Ok(stats);
    }
}