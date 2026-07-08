using System.Security.Claims;
using namera_API.Common.Responses;
using namera_API.DTOs.Customer;

namespace namera_API.Services.Customer;

public interface ICustomerService
{
    Task<ApiResponse<CustomerProfileResponseDto>> GetProfileAsync(ClaimsPrincipal principal);
    Task<ApiResponse<CustomerProfileResponseDto>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateCustomerProfileRequestDto request);
}
