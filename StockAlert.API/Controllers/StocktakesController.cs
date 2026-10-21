using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/stocktakes")]
public class StocktakesController : ControllerBase
{
    private readonly IStocktakeService _stocktakeService;

    public StocktakesController(
        IStocktakeService stocktakeService)
    {
        _stocktakeService = stocktakeService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(
            await _stocktakeService.GetAllAsync(
                CanSeeExpectedStock()));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var session = await _stocktakeService.GetByIdAsync(
            id,
            CanSeeExpectedStock());

        return session == null
            ? NotFound(new { message = "Stocktake not found." })
            : Ok(session);
    }

    [HttpGet("{id:guid}/barcode/{barcode}")]
    public async Task<IActionResult> FindByBarcode(
        Guid id,
        string barcode)
    {
        try
        {
            var item = await _stocktakeService.FindByBarcodeAsync(
                id,
                barcode,
                CanSeeExpectedStock());

            return item == null
                ? NotFound(new
                {
                    message =
                        "This barcode is not part of the current stocktake snapshot."
                })
                : Ok(item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Owner,Manager")]
    [HttpPost]
    public async Task<IActionResult> Create(
        CreateStocktakeRequest request)
    {
        try
        {
            var id = await _stocktakeService.CreateAsync(
                request,
                GetActor());

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                new { id });
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

    [HttpPost("{id:guid}/items/{itemId:guid}/count")]
    public async Task<IActionResult> SubmitCount(
        Guid id,
        Guid itemId,
        SubmitStocktakeCountRequest request)
    {
        try
        {
            var item = await _stocktakeService.SubmitCountAsync(
                id,
                itemId,
                request,
                GetActor(),
                CanSeeExpectedStock());

            return item == null
                ? NotFound(new { message = "Stocktake item not found." })
                : Ok(item);
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

    [HttpPost("{id:guid}/items/{itemId:guid}/recount")]
    public async Task<IActionResult> SubmitRecount(
        Guid id,
        Guid itemId,
        SubmitStocktakeCountRequest request)
    {
        try
        {
            var item = await _stocktakeService.SubmitRecountAsync(
                id,
                itemId,
                request,
                GetActor(),
                CanSeeExpectedStock());

            return item == null
                ? NotFound(new { message = "Stocktake item not found." })
                : Ok(item);
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

    [Authorize(Roles = "Owner,Manager")]
    [HttpPost("{id:guid}/items/{itemId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid id,
        Guid itemId,
        ResolveStocktakeItemRequest request)
    {
        try
        {
            var item = await _stocktakeService.ResolveAsync(
                id,
                itemId,
                request,
                GetActor());

            return item == null
                ? NotFound(new { message = "Stocktake item not found." })
                : Ok(item);
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

    [Authorize(Roles = "Owner,Manager")]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        try
        {
            var completed = await _stocktakeService.CompleteAsync(
                id,
                GetActor());

            return completed
                ? Ok(new { message = "Stocktake completed." })
                : NotFound(new { message = "Stocktake not found." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool CanSeeExpectedStock()
    {
        return User.IsInRole("Owner")
            || User.IsInRole("Manager");
    }

    private string GetActor()
    {
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? "Unknown User";
    }
}
