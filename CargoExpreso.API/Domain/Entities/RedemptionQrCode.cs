namespace CargoExpreso.API.Domain.Entities;

public class RedemptionQrCode
{
    public Guid     Id                  { get; set; }
    public Guid     RedemptionRequestId { get; set; }
    public Guid     CustomerId          { get; set; }
    public Guid     QrCode              { get; set; } = Guid.NewGuid();
    public decimal  Amount              { get; set; }
    public DateTime GeneratedAt         { get; set; }
    public DateTime ExpiresAt           { get; set; }
    public bool     IsUsed              { get; set; }
    public DateTime? UsedAt             { get; set; }
    public Guid?    UsedByBranchId      { get; set; }
    public Guid?    UsedByOperatorId    { get; set; }
    // [Timestamp] configured in AppDbContext — prevents double-use race condition
    public byte[]   RowVersion          { get; set; } = default!;

    public RedemptionRequest RedemptionRequest { get; set; } = default!;
    public Customer          Customer          { get; set; } = default!;
}
