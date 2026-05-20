using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class AuditLog
{
    public long        Id              { get; set; }
    public string      EntityType      { get; set; } = default!;
    public string?     EntityId        { get; set; }
    public string      OperationType   { get; set; } = default!;
    public Guid?       CustomerId      { get; set; }
    public Guid?       UserId          { get; set; }
    public Guid?       BranchId        { get; set; }
    public string?     OldValues       { get; set; }
    public string?     NewValues       { get; set; }
    public string?     IpAddress       { get; set; }
    public string?     UserAgent       { get; set; }
    public AuditResult Result          { get; set; }
    public string?     RejectionReason { get; set; }
    public DateTime    CreatedAt       { get; set; }
}
