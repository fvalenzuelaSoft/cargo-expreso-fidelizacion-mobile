namespace CargoExpreso.App.Models;

public class PointsBalance
{
    public decimal  Balance        { get; set; }
    public decimal  PendingBalance { get; set; }
    public DateTime LastUpdated    { get; set; }
}

public class PointsTransaction
{
    public Guid     Id              { get; set; }
    public string   TransactionType { get; set; } = "";
    public decimal  Points          { get; set; }
    public string   Description     { get; set; } = "";
    public DateTime TransactionDate { get; set; }
    public string?  ShipmentNumber  { get; set; }

    public bool IsCredit => Points > 0;
    public string PointsDisplay => IsCredit ? $"+{Points:N0}" : $"{Points:N0}";
}

public class PointsHistoryResponse
{
    public List<PointsTransaction> Transactions  { get; set; } = [];
    public int                     TotalCount    { get; set; }
    public decimal                 CurrentBalance { get; set; }
}
