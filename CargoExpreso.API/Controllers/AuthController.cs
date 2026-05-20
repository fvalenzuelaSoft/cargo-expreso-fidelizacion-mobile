using CargoExpreso.API.DTOs.Auth;
using CargoExpreso.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CargoExpreso.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IJwtService  _jwt;

    public AuthController(IAuthService auth, IJwtService jwt)
    {
        _auth = auth;
        _jwt  = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip        = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();
        var result    = await _auth.LoginAsync(request, ip, userAgent);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.RefreshTokenAsync(request, ip);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var customerId = _jwt.GetCustomerIdFromPrincipal(User);
        if (customerId is null) return Unauthorized();

        var result = await _auth.LogoutAsync(request.RefreshToken, customerId.Value);
        return Ok(result);
    }
}
