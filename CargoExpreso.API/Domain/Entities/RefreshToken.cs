namespace CargoExpreso.API.Domain.Entities;

public class RefreshToken
{
    public Guid     Id            { get; set; }
    public string   TokenHash     { get; set; } = default!;
    public Guid?    CustomerId    { get; set; }
    public Guid?    UserId        { get; set; }
    public DateTime ExpiresAt     { get; set; }
    public bool     IsRevoked     { get; set; }
    public DateTime? RevokedAt    { get; set; }
    public string?  RevokedReason { get; set; }
    public DateTime CreatedAt     { get; set; }
    public string?  IpAddress     { get; set; }
    public string?  DeviceInfo    { get; set; }

    public Customer? Customer { get; set; }
    public User?     User     { get; set; }
}
