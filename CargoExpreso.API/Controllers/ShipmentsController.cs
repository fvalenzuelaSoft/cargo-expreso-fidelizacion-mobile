using CargoExpreso.API.DTOs.Shipments;
using CargoExpreso.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoExpreso.API.Controllers;

[ApiController]
[Route("api/v1/shipments")]
[Authorize]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipments;
    private readonly IJwtService      _jwt;

    public ShipmentsController(IShipmentService shipments, IJwtService jwt)
    {
        _shipments = shipments;
        _jwt       = jwt;
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanShipmentRequest request)
    {
        var customerId     = _jwt.GetCustomerIdFromPrincipal(User);
        var identityNumber = _jwt.GetIdentityNumberFromPrincipal(User);

        if (customerId is null || identityNumber is null) return Unauthorized();

        var ip     = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _shipments.ScanAsync(request, customerId.Value, identityNumber, ip);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
