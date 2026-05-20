using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public interface IApiClient
{
    Task<ApiResponse<T>?> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object payload);
    Task<ApiResponse<T>?> PostAnonymousAsync<T>(string endpoint, object payload);
    Task<bool> PostAnonymousVoidAsync(string endpoint, object payload);
}
