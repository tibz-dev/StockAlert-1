using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.DTOs;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/business-profile")]
public class BusinessProfileController : ControllerBase
{
    private readonly IBusinessProfileService _businessProfileService;

    public BusinessProfileController(
        IBusinessProfileService businessProfileService)
    {
        _businessProfileService = businessProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(await _businessProfileService.GetAsync());
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        UpdateBusinessProfileRequest request)
    {
        try
        {
            return Ok(await _businessProfileService.UpdateAsync(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
