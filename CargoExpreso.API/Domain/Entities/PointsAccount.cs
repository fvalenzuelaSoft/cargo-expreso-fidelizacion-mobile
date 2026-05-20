namespace CargoExpreso.API.Domain.Entities;

public class PointsAccount
{
    public Guid     Id               { get; set; }
    public Guid     CustomerId       { get; set; }
    public decimal  Balance          { get; set; }
    public decimal  TotalAccumulated { get; set; }
    public decimal  TotalRedeemed    { get; set; }
    public DateTime? LastActivityAt  { get; set; }
    // [Timestamp] configured in AppDbContext — critical for optimistic concurrency
    public byte[]   RowVersion       { get; set; } = default!;
    public DateTime CreatedAt        { get; set; }
    public DateTime UpdatedAt        { get; set; }

    public Customer Customer { get; set; } = default!;
}
