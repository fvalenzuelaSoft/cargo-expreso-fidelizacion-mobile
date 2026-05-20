using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Redemptions;

namespace CargoExpreso.API.Interfaces;

public interface IRedemptionService
{
    Task<ApiResponse<RedemptionResponse>> CreateAsync(CreateRedemptionRequest request, Guid customerId);
    Task<ApiResponse<RedemptionResponse>> ApplyAsync(ApplyRedemptionRequest request, Guid operatorUserId, string? ipAddress);
}
