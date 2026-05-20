using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class RedemptionRequest
{
    public Guid                    Id                 { get; set; }
    public Guid                    CustomerId         { get; set; }
    public decimal                 RequestedAmount    { get; set; }
    public RedemptionRequestStatus Status             { get; set; } = RedemptionRequestStatus.Pending;
    public DateTime                RequestedAt        { get; set; }
    public DateTime?               CompletedAt        { get; set; }
    public Guid?                   BranchId           { get; set; }
    public Guid?                   OperatorUserId     { get; set; }
    public string?                 CancellationReason { get; set; }

    public Customer          Customer      { get; set; } = default!;
    public Branch?           Branch        { get; set; }
    public User?             OperatorUser  { get; set; }
    public RedemptionQrCode? QrCode        { get; set; }
}
