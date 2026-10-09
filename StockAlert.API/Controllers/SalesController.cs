using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IDocumentDeliveryService _deliveryService;

    public SalesController(
        IProductService productService,
        IDocumentDeliveryService deliveryService)
    {
        _productService = productService;
        _deliveryService = deliveryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var sales = await _productService.GetAllSalesAsync();
        return Ok(sales);
    }

    [HttpPost]
    public async Task<IActionResult> MakeSale(CreateSaleRequest request)
    {
        try
        {
            var salespersonFallback =
                User.FindFirstValue(ClaimTypes.Name)
                ?? User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "Unknown User";

            var receipt = await _productService.RecordSaleAsync(
                request,
                salespersonFallback);

            if (receipt == null)
            {
                return BadRequest(new
                {
                    message = "Invalid quantity or product not found."
                });
            }

            return Ok(receipt);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceipt(Guid id)
    {
        var receipt = await _deliveryService.GetReceiptAsync(id);

        return receipt == null
            ? NotFound(new { message = "Receipt not found." })
            : Ok(receipt);
    }

    [HttpPost("{id:guid}/receipt/delivery")]
    public async Task<IActionResult> PrepareReceiptDelivery(
        Guid id,
        PrepareDeliveryRequest request)
    {
        try
        {
            return Ok(
                await _deliveryService.PrepareReceiptAsync(id, request));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
}
