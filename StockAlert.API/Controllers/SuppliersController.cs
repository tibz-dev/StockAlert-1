using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _supplierService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var supplier = await _supplierService.GetByIdAsync(id);

        return supplier == null
            ? NotFound(new { message = "Supplier not found." })
            : Ok(supplier);
    }

    [HttpGet("{id:guid}/products")]
    public async Task<IActionResult> GetProducts(Guid id)
    {
        return Ok(await _supplierService.GetProductsAsync(id));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSupplierRequest request)
    {
        try
        {
            var id = await _supplierService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateSupplierRequest request)
    {
        try
        {
            var updated = await _supplierService.UpdateAsync(id, request);

            return updated
                ? NoContent()
                : NotFound(new { message = "Supplier not found." });
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _supplierService.DeleteAsync(id);

            return deleted
                ? NoContent()
                : NotFound(new { message = "Supplier not found." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
