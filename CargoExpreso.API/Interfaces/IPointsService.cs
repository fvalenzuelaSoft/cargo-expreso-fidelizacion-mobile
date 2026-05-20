using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Points;

namespace CargoExpreso.API.Interfaces;

public interface IPointsService
{
    Task<ApiResponse<PointsAccountResponse>>    GetBalanceAsync(Guid customerId);
    Task<ApiResponse<List<TransactionResponse>>> GetHistoryAsync(Guid customerId, int page, int pageSize);
}
