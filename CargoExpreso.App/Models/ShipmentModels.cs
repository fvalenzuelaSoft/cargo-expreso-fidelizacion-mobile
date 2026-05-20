namespace CargoExpreso.App.Models;

public class ScanShipmentRequest
{
    public string ShipmentNumber { get; set; } = "";
}

public class ScanShipmentResponse
{
    public string  ShipmentNumber { get; set; } = "";
    public decimal PointsAwarded  { get; set; }
    public decimal NewBalance     { get; set; }
    public string  Message        { get; set; } = "";
}
