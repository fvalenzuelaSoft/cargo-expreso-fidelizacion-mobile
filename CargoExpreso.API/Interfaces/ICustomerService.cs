using CargoExpreso.API.DTOs.Common;
using CargoExpreso.API.DTOs.Customers;

namespace CargoExpreso.API.Interfaces;

public interface ICustomerService
{
    Task<ApiResponse<CustomerResponse>> RegisterAsync(RegisterCustomerRequest request, string? ipAddress);
    Task<ApiResponse<CustomerResponse>> GetProfileAsync(Guid customerId);
    Task<ApiResponse<CustomerResponse>> UpdateProfileAsync(Guid customerId, UpdateProfileRequest request, string? ipAddress);
}
