using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool activeOnly = false)
    {
        return Ok(await _staffService.GetAllAsync(activeOnly));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var staff = await _staffService.GetByIdAsync(id);

        return staff == null
            ? NotFound(new { message = "Staff member not found." })
            : Ok(staff);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateStaffMemberRequest request)
    {
        try
        {
            var id = await _staffService.CreateAsync(request);
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

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateStaffMemberRequest request)
    {
        try
        {
            var staff = await _staffService.UpdateAsync(id, request);

            return staff == null
                ? NotFound(new { message = "Staff member not found." })
                : Ok(staff);
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
