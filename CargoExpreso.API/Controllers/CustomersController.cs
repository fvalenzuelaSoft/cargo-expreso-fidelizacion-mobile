using CargoExpreso.API.DTOs.Customers;
using CargoExpreso.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoExpreso.API.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;
    private readonly IJwtService      _jwt;

    public CustomersController(ICustomerService customers, IJwtService jwt)
    {
        _customers = customers;
        _jwt       = jwt;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerRequest request)
    {
        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _customers.RegisterAsync(request, ip);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var customerId = _jwt.GetCustomerIdFromPrincipal(User);
        if (customerId is null) return Unauthorized();

        var result = await _customers.GetProfileAsync(customerId.Value);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var customerId = _jwt.GetCustomerIdFromPrincipal(User);
        if (customerId is null) return Unauthorized();

        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _customers.UpdateProfileAsync(customerId.Value, request, ip);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
