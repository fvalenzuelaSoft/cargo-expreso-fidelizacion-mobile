namespace CargoExpreso.API.DTOs.Configurations;

public class ConfigurationResponse
{
    public int    Id          { get; set; }
    public string ConfigKey   { get; set; } = default!;
    public string ConfigValue { get; set; } = default!;
    public string DataType    { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Module      { get; set; } = default!;
    public bool   IsEditable  { get; set; }
    public string? MinValue   { get; set; }
    public string? MaxValue   { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string  UpdatedBy  { get; set; } = default!;
}

public class UpdateConfigurationRequest
{
    public string ConfigValue { get; set; } = default!;
}
