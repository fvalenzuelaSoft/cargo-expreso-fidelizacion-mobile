namespace CargoExpreso.API.Domain.Entities;

public class SystemConfiguration
{
    public int     Id          { get; set; }
    public string  ConfigKey   { get; set; } = default!;
    public string  ConfigValue { get; set; } = default!;
    public string  DataType    { get; set; } = default!;
    public string  Description { get; set; } = default!;
    public string  Module      { get; set; } = default!;
    public bool    IsEditable  { get; set; } = true;
    public bool    IsActive    { get; set; } = true;
    public string? MinValue    { get; set; }
    public string? MaxValue    { get; set; }
    public DateTime UpdatedAt  { get; set; }
    public string  UpdatedBy   { get; set; } = "SYSTEM";
}
