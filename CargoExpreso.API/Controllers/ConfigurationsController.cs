using CargoExpreso.API.Data;
using CargoExpreso.API.Domain.Entities;
using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Configurations;
using CargoExpreso.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoExpreso.API.Controllers;

[ApiController]
[Route("api/v1/configurations")]
[Authorize(Roles = "Admin,Supervisor")]
public class ConfigurationsController : ControllerBase
{
    private readonly AppDbContext          _db;
    private readonly IConfigurationService _config;
    private readonly IJwtService           _jwt;

    public ConfigurationsController(AppDbContext db, IConfigurationService config, IJwtService jwt)
    {
        _db     = db;
        _config = config;
        _jwt    = jwt;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? module = null)
    {
        var query = _db.SystemConfigurations.AsNoTracking().Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(module))
            query = query.Where(c => c.Module == module);

        var result = await query
            .OrderBy(c => c.Module).ThenBy(c => c.ConfigKey)
            .Select(c => new ConfigurationResponse
            {
                Id          = c.Id,
                ConfigKey   = c.ConfigKey,
                ConfigValue = c.ConfigValue,
                DataType    = c.DataType,
                Description = c.Description,
                Module      = c.Module,
                IsEditable  = c.IsEditable,
                MinValue    = c.MinValue,
                MaxValue    = c.MaxValue,
                UpdatedAt   = c.UpdatedAt,
                UpdatedBy   = c.UpdatedBy
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ConfigurationResponse>>.Ok(result));
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateConfigurationRequest request)
    {
        var config = await _db.SystemConfigurations.FirstOrDefaultAsync(c => c.ConfigKey == key);
        if (config is null)    return NotFound(ApiResponse<bool>.Fail("Configuración no encontrada"));
        if (!config.IsEditable) return BadRequest(ApiResponse<bool>.Fail("Esta configuración no es editable"));

        // Range validation
        if (!ValidateRange(request.ConfigValue, config.DataType, config.MinValue, config.MaxValue, out var rangeError))
            return BadRequest(ApiResponse<bool>.Fail(rangeError!));

        var oldValue   = config.ConfigValue;
        var operatorId = _jwt.GetCustomerIdFromPrincipal(User);
        var username   = User.Identity?.Name ?? operatorId?.ToString() ?? "unknown";
        var ip         = HttpContext.Connection.RemoteIpAddress?.ToString();

        config.ConfigValue = request.ConfigValue;
        config.UpdatedAt   = DateTime.UtcNow;
        config.UpdatedBy   = username;

        _db.SystemConfigurationAudits.Add(new SystemConfigurationAudit
        {
            ConfigKey  = key,
            OldValue   = oldValue,
            NewValue   = request.ConfigValue,
            ChangedAt  = DateTime.UtcNow,
            ChangedBy  = username,
            IpAddress  = ip
        });

        await _db.SaveChangesAsync();
        _config.InvalidateCache(key);

        return Ok(ApiResponse<bool>.Ok(true, $"Configuración '{key}' actualizada correctamente"));
    }

    private static bool ValidateRange(string value, string dataType, string? min, string? max, out string? error)
    {
        error = null;
        try
        {
            if (dataType == "Decimal")
            {
                var v = decimal.Parse(value);
                if (min is not null && v < decimal.Parse(min)) { error = $"Valor mínimo permitido: {min}"; return false; }
                if (max is not null && v > decimal.Parse(max)) { error = $"Valor máximo permitido: {max}"; return false; }
            }
            else if (dataType == "Int")
            {
                var v = int.Parse(value);
                if (min is not null && v < int.Parse(min)) { error = $"Valor mínimo permitido: {min}"; return false; }
                if (max is not null && v > int.Parse(max)) { error = $"Valor máximo permitido: {max}"; return false; }
            }
            return true;
        }
        catch { error = $"Valor inválido para tipo de dato '{dataType}'"; return false; }
    }
}
