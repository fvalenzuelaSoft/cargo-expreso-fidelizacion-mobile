using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public class AuthService : IAuthService
{
    private readonly IApiClient _api;

    public AuthService(IApiClient api) => _api = api;

    public async Task<ApiResponse<LoginResponse>?> LoginAsync(LoginRequest request)
    {
        var result = await _api.PostAnonymousAsync<LoginResponse>(Constants.Routes.Login, request);

        if (result?.Success == true && result.Data is not null)
        {
            await SecureStorageHelper.SaveTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);
            await SecureStorageHelper.SaveCustomerAsync(
                result.Data.Customer.Id.ToString(),
                result.Data.Customer.FullName);
        }

        return result;
    }

    public async Task<ApiResponse<CustomerProfile>?> RegisterAsync(RegisterCustomerRequest request) =>
        await _api.PostAnonymousAsync<CustomerProfile>(Constants.Routes.Register, request);

    public async Task<bool> RefreshTokenAsync(string refreshToken)
    {
        var result = await _api.PostAnonymousAsync<LoginResponse>(
            Constants.Routes.Refresh,
            new RefreshTokenRequest { RefreshToken = refreshToken });

        if (result?.Success == true && result.Data is not null)
        {
            await SecureStorageHelper.SaveTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);
            return true;
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        var refreshToken = await SecureStorageHelper.GetRefreshTokenAsync();
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _api.PostAnonymousVoidAsync(
                Constants.Routes.Logout,
                new RefreshTokenRequest { RefreshToken = refreshToken });
        }

        SecureStorageHelper.ClearAll();
    }
}
