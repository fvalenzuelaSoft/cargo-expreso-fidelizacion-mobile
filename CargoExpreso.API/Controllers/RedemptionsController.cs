using CargoExpreso.API.DTOs.Redemptions;
using CargoExpreso.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoExpreso.API.Controllers;

[ApiController]
[Route("api/v1/redemptions")]
[Authorize]
public class RedemptionsController : ControllerBase
{
    private readonly IRedemptionService _redemptions;
    private readonly IJwtService        _jwt;

    public RedemptionsController(IRedemptionService redemptions, IJwtService jwt)
    {
        _redemptions = redemptions;
        _jwt         = jwt;
    }

    /// <summary>Customer: generates a QR code for redemption at a branch.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRedemptionRequest request)
    {
        var customerId = _jwt.GetCustomerIdFromPrincipal(User);
        if (customerId is null) return Unauthorized();

        var result = await _redemptions.CreateAsync(request, customerId.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Cashier: applies the QR redemption at a branch terminal.</summary>
    [HttpPost("apply")]
    [Authorize(Roles = "Cashier,Supervisor,Admin")]
    public async Task<IActionResult> Apply([FromBody] ApplyRedemptionRequest request)
    {
        var operatorId = _jwt.GetCustomerIdFromPrincipal(User);
        if (operatorId is null) return Unauthorized();

        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _redemptions.ApplyAsync(request, operatorId.Value, ip);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
