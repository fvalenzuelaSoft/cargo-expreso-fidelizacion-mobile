namespace CargoExpreso.API.DTOs.Redemptions;

public record CreateRedemptionRequest(decimal Amount);

public record ApplyRedemptionRequest(Guid QrCode, Guid BranchId);

public class RedemptionResponse
{
    public Guid     RequestId        { get; set; }
    public Guid?    QrCodeValue      { get; set; }
    public string?  QrCodeBase64     { get; set; }
    public decimal  Amount           { get; set; }
    public DateTime? ExpiresAt       { get; set; }
    public decimal  RemainingBalance { get; set; }
    public string   Status           { get; set; } = default!;
}
