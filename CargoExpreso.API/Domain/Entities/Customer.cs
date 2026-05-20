using CargoExpreso.API.Domain;

namespace CargoExpreso.API.Domain.Entities;

public class Customer
{
    public Guid           Id                  { get; set; }
    public string         IdentityNumber      { get; set; } = default!;
    public string         FirstName           { get; set; } = default!;
    public string         LastName            { get; set; } = default!;
    public string         Phone               { get; set; } = default!;
    public CustomerStatus Status              { get; set; } = CustomerStatus.Active;
    public int            FailedLoginAttempts { get; set; }
    public DateTime?      LockedUntil         { get; set; }
    public DateTime?      LastLoginAt         { get; set; }
    public string?        DeviceToken         { get; set; }
    public byte[]         RowVersion          { get; set; } = default!;
    public DateTime       CreatedAt           { get; set; }
    public DateTime       UpdatedAt           { get; set; }

    public CustomerProfile? Profile       { get; set; }
    public PointsAccount?   PointsAccount { get; set; }
}
