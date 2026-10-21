using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BarcodesController : ControllerBase
{
    private readonly IBarcodeLookupService _barcodeLookupService;

    public BarcodesController(IBarcodeLookupService barcodeLookupService)
    {
        _barcodeLookupService = barcodeLookupService;
    }

    [HttpGet("{barcode}")]
    public async Task<IActionResult> Lookup(string barcode)
    {
        try
        {
            return Ok(await _barcodeLookupService.LookupAsync(barcode));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
