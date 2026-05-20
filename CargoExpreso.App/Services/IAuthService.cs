using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>?> LoginAsync(LoginRequest request);
    Task<ApiResponse<CustomerProfile>?> RegisterAsync(RegisterCustomerRequest request);
    Task<bool> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync();
}
