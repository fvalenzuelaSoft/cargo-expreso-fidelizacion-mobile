namespace CargoExpreso.API.Domain.Entities;

public class Country
{
    public byte   Id       { get; set; }
    public string Code     { get; set; } = default!;
    public string Name     { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public string TimeZone { get; set; } = default!;
    public bool   IsActive { get; set; } = true;
}
