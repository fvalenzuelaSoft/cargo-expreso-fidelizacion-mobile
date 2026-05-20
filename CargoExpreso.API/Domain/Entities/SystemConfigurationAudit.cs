namespace CargoExpreso.API.Domain.Entities;

public class SystemConfigurationAudit
{
    public long     Id        { get; set; }
    public string   ConfigKey { get; set; } = default!;
    public string   OldValue  { get; set; } = default!;
    public string   NewValue  { get; set; } = default!;
    public DateTime ChangedAt { get; set; }
    public string   ChangedBy { get; set; } = default!;
    public string?  IpAddress { get; set; }
}
