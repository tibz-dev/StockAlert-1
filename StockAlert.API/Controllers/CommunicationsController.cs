using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAlert.Application.Interfaces;

namespace StockAlert.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CommunicationsController : ControllerBase
{
    private readonly IOutboundMessageSender _messageSender;

    public CommunicationsController(
        IOutboundMessageSender messageSender)
    {
        _messageSender = messageSender;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(_messageSender.GetStatus());
    }
}
