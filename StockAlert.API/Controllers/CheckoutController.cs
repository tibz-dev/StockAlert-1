using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/checkout")]
public sealed class CheckoutController : ControllerBase
{
    private readonly IProductService _products;

    public CheckoutController(IProductService products) => _products = products;

    // A non-posting preview shared by Next.js and WPF. This does not reserve
    // inventory, capture a tender, create a receipt or commit a sale.
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] CheckoutPreviewRequest request)
    {
        if (request.Items is not { Count: > 0 and <= 100 })
            return BadRequest(new { message = "A cart must contain 1 to 100 distinct products." });

        if (request.Items.Any(item => item.ProductId == Guid.Empty || item.Quantity <= 0))
            return BadRequest(new { message = "Each cart item requires a product and positive quantity." });

        if (request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
            return BadRequest(new { message = "Combine duplicate products before previewing." });

        var catalog = (await _products.GetAllProductsAsync()).ToDictionary(p => p.Id);
        var lines = new List<CheckoutPreviewLineDto>();

        foreach (var item in request.Items)
        {
            if (!catalog.TryGetValue(item.ProductId, out var product))
                return BadRequest(new { message = $"Product {item.ProductId} is unavailable." });
            if (item.Quantity > product.AvailableQuantity)
                return Conflict(new { message = $"Insufficient available stock for {product.Name}." });

            decimal amount;
            try { amount = checked(product.Price * item.Quantity); }
            catch (OverflowException) { return BadRequest(new { message = "Cart amount exceeds supported limits." }); }
            lines.Add(new CheckoutPreviewLineDto(item.ProductId, product.Name,
                product.Barcode, item.Quantity, product.Price, amount, product.AvailableQuantity));
        }

        decimal subtotal;
        try { subtotal = checked(lines.Sum(line => line.LineTotal)); }
        catch (OverflowException) { return BadRequest(new { message = "Cart total exceeds supported limits." }); }

        return Ok(new CheckoutPreviewDto(lines, subtotal, lines.Sum(x => x.Quantity),
            DateTime.UtcNow,
            "Estimate only. Final VAT, payment verification and stock deduction require transactional checkout."));
    }
}
