namespace CargoExpreso.App.Models;

public class CreateRedemptionRequest
{
    public decimal Amount { get; set; }
}

public class CreateRedemptionResponse
{
    public Guid     RedemptionId      { get; set; }
    public string   QrCodeBase64      { get; set; } = "";
    public decimal  Amount            { get; set; }
    public DateTime ExpiresAt         { get; set; }
    public decimal  RemainingBalance  { get; set; }
}
