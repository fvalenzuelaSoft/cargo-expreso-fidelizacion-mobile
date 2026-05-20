using CargoExpreso.App.Models;

namespace CargoExpreso.App.Services;

public class PointsService : IPointsService
{
    private readonly IApiClient _api;

    public PointsService(IApiClient api) => _api = api;

    public Task<ApiResponse<PointsBalance>?> GetBalanceAsync() =>
        _api.GetAsync<PointsBalance>(Constants.Routes.PointsBalance);

    public Task<ApiResponse<PointsHistoryResponse>?> GetHistoryAsync(int page = 1, int pageSize = 20) =>
        _api.GetAsync<PointsHistoryResponse>(
            $"{Constants.Routes.PointsHistory}?page={page}&pageSize={pageSize}");

    public Task<ApiResponse<ScanShipmentResponse>?> ScanShipmentAsync(string shipmentNumber) =>
        _api.PostAsync<ScanShipmentResponse>(
            Constants.Routes.ShipmentScan,
            new ScanShipmentRequest { ShipmentNumber = shipmentNumber });
}
