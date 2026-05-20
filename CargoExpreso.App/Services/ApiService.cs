using CargoExpreso.App.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CargoExpreso.App.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ApiClient()
    {
        _http = new HttpClient { BaseAddress = new Uri(Constants.ApiBaseUrl) };
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        await AttachTokenAsync();
        var response = await _http.GetAsync(endpoint);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshAsync())
            response = await _http.GetAsync(endpoint);

        return await ReadAsync<T>(response);
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object payload)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync(endpoint, payload, _json);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && await TryRefreshAsync())
            response = await _http.PostAsJsonAsync(endpoint, payload, _json);

        return await ReadAsync<T>(response);
    }

    public async Task<ApiResponse<T>?> PostAnonymousAsync<T>(string endpoint, object payload)
    {
        var response = await _http.PostAsJsonAsync(endpoint, payload, _json);
        return await ReadAsync<T>(response);
    }

    public async Task<bool> PostAnonymousVoidAsync(string endpoint, object payload)
    {
        try
        {
            await AttachTokenAsync();
            var response = await _http.PostAsJsonAsync(endpoint, payload, _json);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private async Task AttachTokenAsync()
    {
        var token = await SecureStorageHelper.GetAccessTokenAsync();
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<bool> TryRefreshAsync()
    {
        var refreshToken = await SecureStorageHelper.GetRefreshTokenAsync();
        if (string.IsNullOrEmpty(refreshToken)) return false;

        try
        {
            var response = await _http.PostAsJsonAsync(
                Constants.Routes.Refresh,
                new { RefreshToken = refreshToken },
                _json);

            if (!response.IsSuccessStatusCode) return false;

            var content = await response.Content.ReadAsStringAsync();
            var result  = JsonSerializer.Deserialize<ApiResponse<Models.LoginResponse>>(content, _json);

            if (result?.Success != true || result.Data is null) return false;

            await SecureStorageHelper.SaveTokensAsync(result.Data.AccessToken, result.Data.RefreshToken);
            await AttachTokenAsync();
            return true;
        }
        catch { return false; }
    }

    private static async Task<ApiResponse<T>?> ReadAsync<T>(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse<T>>(content, _json);
        }
        catch { return null; }
    }
}
