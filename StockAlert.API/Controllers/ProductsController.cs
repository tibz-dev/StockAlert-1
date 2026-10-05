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

        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request)
    {
        try
        {
            var id = await _productService.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateProductRequest request)
    {
        try
        {
            var updated = await _productService.UpdateProductAsync(id, request);

            return updated
                ? NoContent()
                : NotFound(new { message = "Product not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _productService.DeleteProductAsync(id);

            return deleted
                ? NoContent()
                : NotFound(new { message = "Product not found." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/stock-adjustments")]
    public async Task<IActionResult> AdjustStock(
        Guid id,
        AdjustStockRequest request)
    {
        try
        {
            var adjusted = await _productService.AdjustStockAsync(id, request);

            return adjusted
                ? Ok(await _productService.GetProductByIdAsync(id))
                : NotFound(new { message = "Product not found." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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
