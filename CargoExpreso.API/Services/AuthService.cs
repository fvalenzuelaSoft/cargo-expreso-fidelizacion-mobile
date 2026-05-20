using System.Security.Cryptography;
using System.Text;
using CargoExpreso.API.Data;
using CargoExpreso.API.Domain;
using CargoExpreso.API.Domain.Entities;
using CargoExpreso.API.DTOs.Auth;
using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CargoExpreso.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext          _db;
    private readonly IJwtService           _jwt;
    private readonly IConfigurationService _config;
    private readonly IAuditService         _audit;

    public AuthService(AppDbContext db, IJwtService jwt, IConfigurationService config, IAuditService audit)
    {
        _db     = db;
        _jwt    = jwt;
        _config = config;
        _audit  = audit;
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var maxAttempts  = await _config.GetIntAsync("MAX_INTENTOS_LOGIN");
        var lockMinutes  = await _config.GetIntAsync("TIEMPO_BLOQUEO_MINUTOS");
        var tokenMinutes = await _config.GetIntAsync("ACCESS_TOKEN_MINUTOS");
        var refreshDays  = await _config.GetIntAsync("REFRESH_TOKEN_DIAS");

        // Record attempt
        var attempt = new LoginAttempt
        {
            IdentityNumber = request.IdentityNumber,
            IpAddress      = ipAddress,
            DeviceInfo     = request.DeviceInfo,
            UserType       = UserType.Customer,
            AttemptedAt    = DateTime.UtcNow
        };

        var customer = await _db.Customers
            .Include(c => c.Profile)
            .Include(c => c.PointsAccount)
            .FirstOrDefaultAsync(c => c.IdentityNumber == request.IdentityNumber);

        if (customer is null || customer.Phone != request.Phone)
        {
            attempt.IsSuccessful = false;
            attempt.FailureReason = "NotFound";
            _db.LoginAttempts.Add(attempt);
            await _db.SaveChangesAsync();
            await _audit.LogAsync("Auth", request.IdentityNumber, "Login", AuditResult.Rejected,
                rejectionReason: "InvalidCredentials", ipAddress: ipAddress, userAgent: userAgent);
            return ApiResponse<LoginResponse>.Fail("Credenciales inválidas");
        }

        // Check lock
        if (customer.LockedUntil.HasValue && customer.LockedUntil > DateTime.UtcNow)
        {
            attempt.IsSuccessful = false;
            attempt.FailureReason = "AccountLocked";
            _db.LoginAttempts.Add(attempt);
            await _db.SaveChangesAsync();
            return ApiResponse<LoginResponse>.Fail($"Cuenta bloqueada. Intente nuevamente después de {customer.LockedUntil:HH:mm} UTC");
        }

        if (customer.Status == CustomerStatus.Suspended)
        {
            attempt.IsSuccessful = false;
            attempt.FailureReason = "AccountSuspended";
            _db.LoginAttempts.Add(attempt);
            await _db.SaveChangesAsync();
            return ApiResponse<LoginResponse>.Fail("Cuenta suspendida. Contacte soporte.");
        }

        // Success — reset failed attempts, update lock state
        customer.FailedLoginAttempts = 0;
        customer.LockedUntil         = null;
        customer.LastLoginAt         = DateTime.UtcNow;
        customer.UpdatedAt           = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.DeviceInfo))
            customer.DeviceToken = request.DeviceInfo;

        // Generate tokens
        var accessToken             = _jwt.GenerateAccessToken(customer.Id, customer.IdentityNumber, "Customer", tokenMinutes);
        var (refreshToken, rtHash)  = _jwt.GenerateRefreshToken();

        // Revoke old active refresh tokens for this customer (single session)
        await _db.RefreshTokens
            .Where(t => t.CustomerId == customer.Id && !t.IsRevoked)
            .ExecuteUpdateAsync(t => t
                .SetProperty(x => x.IsRevoked,     true)
                .SetProperty(x => x.RevokedAt,     DateTime.UtcNow)
                .SetProperty(x => x.RevokedReason, "Replaced"));

        _db.RefreshTokens.Add(new RefreshToken
        {
            CustomerId = customer.Id,
            TokenHash  = rtHash,
            ExpiresAt  = DateTime.UtcNow.AddDays(refreshDays),
            IpAddress  = ipAddress,
            DeviceInfo = request.DeviceInfo,
            CreatedAt  = DateTime.UtcNow
        });

        attempt.IsSuccessful = true;
        _db.LoginAttempts.Add(attempt);

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Auth", customer.Id.ToString(), "Login", AuditResult.Success,
            customerId: customer.Id, ipAddress: ipAddress, userAgent: userAgent);

        var expiresAt = DateTime.UtcNow.AddMinutes(tokenMinutes);

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            CustomerId   = customer.Id,
            FullName     = $"{customer.FirstName} {customer.LastName}",
            Balance      = customer.PointsAccount?.Balance ?? 0,
            ExpiresAt    = expiresAt
        });
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));

        var stored = await _db.RefreshTokens
            .Include(t => t.Customer)
                .ThenInclude(c => c!.PointsAccount)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow || stored.CustomerId is null)
            return ApiResponse<LoginResponse>.Fail("Refresh token inválido o expirado");

        var customer     = stored.Customer!;
        var tokenMinutes = await _config.GetIntAsync("ACCESS_TOKEN_MINUTOS");
        var refreshDays  = await _config.GetIntAsync("REFRESH_TOKEN_DIAS");

        // Rotate refresh token
        stored.IsRevoked     = true;
        stored.RevokedAt     = DateTime.UtcNow;
        stored.RevokedReason = "Replaced";

        var accessToken            = _jwt.GenerateAccessToken(customer.Id, customer.IdentityNumber, "Customer", tokenMinutes);
        var (refreshToken, rtHash) = _jwt.GenerateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            CustomerId = customer.Id,
            TokenHash  = rtHash,
            ExpiresAt  = DateTime.UtcNow.AddDays(refreshDays),
            IpAddress  = ipAddress,
            CreatedAt  = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return ApiResponse<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken,
            CustomerId   = customer.Id,
            FullName     = $"{customer.FirstName} {customer.LastName}",
            Balance      = customer.PointsAccount?.Balance ?? 0,
            ExpiresAt    = DateTime.UtcNow.AddMinutes(tokenMinutes)
        });
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken, Guid customerId)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        await _db.RefreshTokens
            .Where(t => t.TokenHash == tokenHash && t.CustomerId == customerId && !t.IsRevoked)
            .ExecuteUpdateAsync(t => t
                .SetProperty(x => x.IsRevoked,     true)
                .SetProperty(x => x.RevokedAt,     DateTime.UtcNow)
                .SetProperty(x => x.RevokedReason, "Logout"));

        return ApiResponse<bool>.Ok(true, "Sesión cerrada correctamente");
    }
}
