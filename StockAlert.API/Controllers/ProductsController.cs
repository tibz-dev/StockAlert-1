using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetProductByIdAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        var id = await _productService.CreateProductAsync(request);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("sync-smarttrade")]
    public async Task<IActionResult> SyncSmartTrade()
    {
        var updatedItems = await _productService.SyncWithSmartTradeAsync();

        return Ok(new
        {
            message = $"Sync complete. {updatedItems} products updated.",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("report/csv")]
    public async Task<IActionResult> DownloadReport()
    {
        var fileBytes = await _productService.GenerateStockReportAsync();
        var fileName = $"StockReport_{DateTime.UtcNow:yyyyMMdd}.csv";

        return File(fileBytes, "text/csv", fileName);
    }
}
