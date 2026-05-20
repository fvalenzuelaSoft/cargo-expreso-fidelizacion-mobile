using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class PointsTransaction
{
    public Guid            Id                 { get; set; }
    public Guid            PointsAccountId    { get; set; }
    public Guid            CustomerId         { get; set; }
    public TransactionType TransactionType    { get; set; }
    public decimal         Amount             { get; set; }
    public decimal         BalanceBefore      { get; set; }
    public decimal         BalanceAfter       { get; set; }
    public Guid?           ShipmentId         { get; set; }
    public Guid?           RedemptionQrCodeId { get; set; }
    public Guid?           BranchId           { get; set; }
    public Guid?           OperatorUserId     { get; set; }
    public string?         Notes              { get; set; }
    public string?         IpAddress          { get; set; }
    public DateTime        CreatedAt          { get; set; }

    public PointsAccount PointsAccount  { get; set; } = default!;
    public Customer      Customer       { get; set; } = default!;
    public Shipment?     Shipment       { get; set; }
    public Branch?       Branch         { get; set; }
    public User?         OperatorUser   { get; set; }
}
