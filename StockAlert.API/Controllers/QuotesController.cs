using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quoteService;
    private readonly IDocumentDeliveryService _deliveryService;

    public QuotesController(
        IQuoteService quoteService,
        IDocumentDeliveryService deliveryService)
    {
        _quoteService = quoteService;
        _deliveryService = deliveryService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _quoteService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var quote = await _quoteService.GetByIdAsync(id);

        return quote == null
            ? NotFound(new { message = "Quote not found." })
            : Ok(quote);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateQuoteRequest request)
    {
        try
        {
            var createdBy =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "Unknown User";

            var id = await _quoteService.CreateAsync(request, createdBy);

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        UpdateQuoteStatusRequest request)
    {
        try
        {
            var quote = await _quoteService.UpdateStatusAsync(id, request);

            return quote == null
                ? NotFound(new { message = "Quote not found." })
                : Ok(quote);
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

    [HttpPost("{id:guid}/delivery")]
    public async Task<IActionResult> PrepareDelivery(
        Guid id,
        PrepareDeliveryRequest request)
    {
        try
        {
            return Ok(await _deliveryService.PrepareQuoteAsync(id, request));
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

    [HttpPost("{id:guid}/convert-to-sale")]
    public async Task<IActionResult> ConvertToSale(Guid id)
    {
        try
        {
            var result = await _quoteService.ConvertToSaleAsync(id);

            return result == null
                ? NotFound(new { message = "Quote not found." })
                : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
