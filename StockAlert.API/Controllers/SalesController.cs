using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly IProductService _productService;

    public SalesController(IProductService productService)
    {
        _productService = productService;
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var sales = await _productService.GetAllProductsAsync();
        return Ok(sales);
    }
    [HttpPost]
    public async Task<IActionResult> MakeSale(CreateSaleRequest request)
    {
        var success = await _productService.RecordSaleAsync(request);

        if (!success)
        {
            return BadRequest("Insufficient stock or product not found.");
        }

        return Ok("Sale recorded successfully.");
    }
}