using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class Shipment
{
    public Guid           Id                    { get; set; }
    public string         ShipmentNumber        { get; set; } = default!;
    public string         OwnerIdentityNumber   { get; set; } = default!;
    public decimal        ShipmentAmount        { get; set; }
    public ShipmentStatus Status                { get; set; } = ShipmentStatus.Pending;
    public DateTime       IssuedAt              { get; set; }
    public DateTime       ExpiresAt             { get; set; }
    public DateTime?      ScannedAt             { get; set; }
    public Guid?          ScannedByCustomerId   { get; set; }
    public decimal        PointsAwarded         { get; set; }
    public string         SourceSystem          { get; set; } = "API_CENTRAL_CE";
    public DateTime       CreatedAt             { get; set; }

    public Customer? ScannedByCustomer { get; set; }
}
