using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/stock-movements")]
public class StockMovementsController : ControllerBase
{
    private readonly IStockMovementService _stockMovementService;

    public StockMovementsController(IStockMovementService stockMovementService)
    {
        _stockMovementService = stockMovementService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? productId = null,
        [FromQuery] int take = 100)
    {
        var movements = await _stockMovementService.GetAsync(productId, take);
        return Ok(movements);
    }
}
