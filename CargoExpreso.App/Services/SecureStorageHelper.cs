namespace CargoExpreso.App.Services;

public static class SecureStorageHelper
{
    private const string KeyAccessToken  = "access_token";
    private const string KeyRefreshToken = "refresh_token";
    private const string KeyCustomerId   = "customer_id";
    private const string KeyCustomerName = "customer_name";

    public static async Task SaveTokensAsync(string accessToken, string refreshToken)
    {
        await SecureStorage.Default.SetAsync(KeyAccessToken,  accessToken);
        await SecureStorage.Default.SetAsync(KeyRefreshToken, refreshToken);
    }

    public static Task<string?> GetAccessTokenAsync()  => SecureStorage.Default.GetAsync(KeyAccessToken);
    public static Task<string?> GetRefreshTokenAsync() => SecureStorage.Default.GetAsync(KeyRefreshToken);

    public static async Task SaveCustomerAsync(string customerId, string customerName)
    {
        await SecureStorage.Default.SetAsync(KeyCustomerId,   customerId);
        await SecureStorage.Default.SetAsync(KeyCustomerName, customerName);
    }

    public static Task<string?> GetCustomerIdAsync()   => SecureStorage.Default.GetAsync(KeyCustomerId);
    public static Task<string?> GetCustomerNameAsync() => SecureStorage.Default.GetAsync(KeyCustomerName);

    public static void ClearAll()
    {
        SecureStorage.Default.Remove(KeyAccessToken);
        SecureStorage.Default.Remove(KeyRefreshToken);
        SecureStorage.Default.Remove(KeyCustomerId);
        SecureStorage.Default.Remove(KeyCustomerName);
    }

    public static async Task<bool> IsAuthenticatedAsync() =>
        !string.IsNullOrEmpty(await GetAccessTokenAsync());
}
