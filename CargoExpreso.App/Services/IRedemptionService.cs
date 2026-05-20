using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public interface IRedemptionService
{
    Task<ApiResponse<CreateRedemptionResponse>?> CreateRedemptionAsync(decimal amount);
}
