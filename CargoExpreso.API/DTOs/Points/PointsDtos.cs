using CargoExpreso.API.Domain;

namespace CargoExpreso.API.DTOs.Points;

public class PointsAccountResponse
{
    public Guid    AccountId        { get; set; }
    public decimal Balance          { get; set; }
    public decimal TotalAccumulated { get; set; }
    public decimal TotalRedeemed    { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

public class TransactionResponse
{
    public Guid            Id              { get; set; }
    public string          Type            { get; set; } = default!;
    public decimal         Amount          { get; set; }
    public decimal         BalanceBefore   { get; set; }
    public decimal         BalanceAfter    { get; set; }
    public string?         ShipmentNumber  { get; set; }
    public string?         BranchName      { get; set; }
    public string?         Notes           { get; set; }
    public DateTime        CreatedAt       { get; set; }
}

public class TransactionHistoryRequest
{
    public int Page     { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
