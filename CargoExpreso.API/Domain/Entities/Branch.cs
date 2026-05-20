namespace CargoExpreso.API.Domain.Entities;

public class Branch
{
    public Guid   Id        { get; set; }
    public byte   CountryId { get; set; }
    public string Code      { get; set; } = default!;
    public string Name      { get; set; } = default!;
    public string Address   { get; set; } = default!;
    public string? Phone    { get; set; }
    public bool   IsActive  { get; set; } = true;
    public byte[] RowVersion { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Country Country { get; set; } = default!;
}
