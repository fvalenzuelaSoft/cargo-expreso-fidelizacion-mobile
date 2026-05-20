using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Shipments;

namespace CargoExpreso.API.Interfaces;

public interface IShipmentService
{
    Task<ApiResponse<ShipmentScanResponse>> ScanAsync(
        ScanShipmentRequest request,
        Guid    customerId,
        string  identityNumber,
        string? ipAddress);
}
