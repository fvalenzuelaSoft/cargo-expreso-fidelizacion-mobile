using CargoExpreso.API.DTOs.Points;
using CargoExpreso.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoExpreso.API.Controllers;

[ApiController]
[Route("api/v1/points")]
[Authorize]
public class PointsController : ControllerBase
{
    private readonly IPointsService _points;
    private readonly IJwtService    _jwt;

    public PointsController(IPointsService points, IJwtService jwt)
    {
        _points = points;
        _jwt    = jwt;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var customerId = _jwt.GetCustomerIdFromPrincipal(User);
        if (customerId is null) return Unauthorized();

        var result = await _points.GetBalanceAsync(customerId.Value);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var customerId = _jwt.GetCustomerIdFromPrincipal(User);
        if (customerId is null) return Unauthorized();

        var result = await _points.GetHistoryAsync(customerId.Value, page, pageSize);
        return Ok(result);
    }
}
