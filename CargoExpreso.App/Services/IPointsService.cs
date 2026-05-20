using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public interface IPointsService
{
    Task<ApiResponse<PointsBalance>?> GetBalanceAsync();
    Task<ApiResponse<PointsHistoryResponse>?> GetHistoryAsync(int page = 1, int pageSize = 20);
    Task<ApiResponse<ScanShipmentResponse>?> ScanShipmentAsync(string shipmentNumber);
}
