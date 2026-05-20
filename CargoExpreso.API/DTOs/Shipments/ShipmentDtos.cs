namespace CargoExpreso.API.DTOs.Shipments;

public record ScanShipmentRequest(string ShipmentNumber);

public class ShipmentScanResponse
{
    public string  ShipmentNumber { get; set; } = default!;
    public decimal ShipmentAmount { get; set; }
    public decimal PointsAwarded  { get; set; }
    public decimal NewBalance     { get; set; }
    public string  Message        { get; set; } = default!;
}
