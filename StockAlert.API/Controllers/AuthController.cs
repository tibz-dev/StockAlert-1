using Microsoft.AspNetCore.Mvc;

[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    var result = await _authService.RegisterAsync(request.Email, request.Password, request.FullName);
    if (!result.Success) return BadRequest(result);
    return Ok(result);
}

// Add this DTO to your Application project
public record RegisterRequest(string Email, string Password, string FullName);