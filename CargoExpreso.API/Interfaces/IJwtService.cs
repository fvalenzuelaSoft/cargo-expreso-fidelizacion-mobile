using System.Security.Claims;

namespace CargoExpreso.API.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid customerId, string identityNumber, string role, int expiryMinutes);
    (string token, string tokenHash) GenerateRefreshToken();
    Guid?   GetCustomerIdFromPrincipal(ClaimsPrincipal principal);
    string? GetIdentityNumberFromPrincipal(ClaimsPrincipal principal);
}
