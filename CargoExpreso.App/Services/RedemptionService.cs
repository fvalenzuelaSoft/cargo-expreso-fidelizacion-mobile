using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public class RedemptionService : IRedemptionService
{
    private readonly IApiClient _api;

    public RedemptionService(IApiClient api) => _api = api;

    public Task<ApiResponse<CreateRedemptionResponse>?> CreateRedemptionAsync(decimal amount) =>
        _api.PostAsync<CreateRedemptionResponse>(
            Constants.Routes.Redemptions,
            new CreateRedemptionRequest { Amount = amount });
}
