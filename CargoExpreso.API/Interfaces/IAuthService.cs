using CargoExpreso.API.DTOs.Auth;
using CargoExpreso.API.DTOs.Common;

namespace CargoExpreso.API.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent);
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress);
    Task<ApiResponse<bool>>          LogoutAsync(string refreshToken, Guid customerId);
}
