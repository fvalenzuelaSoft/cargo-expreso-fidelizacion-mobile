namespace CargoExpreso.API.DTOs.Auth;

public record LoginRequest(string IdentityNumber, string Phone, string? DeviceInfo);

public record RefreshTokenRequest(string RefreshToken);

public class LoginResponse
{
    public string  AccessToken  { get; set; } = default!;
    public string  RefreshToken { get; set; } = default!;
    public Guid    CustomerId   { get; set; }
    public string  FullName     { get; set; } = default!;
    public decimal Balance      { get; set; }
    public DateTime ExpiresAt   { get; set; }
}
