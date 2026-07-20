using System.Security.Claims;
using namera_API.Common.Responses;
using namera_API.DTOs.Customer;

namespace namera_API.Services.Customer;

public interface ICustomerService
{
    Task<ApiResponse<CustomerProfileResponseDto>> GetProfileAsync(ClaimsPrincipal principal);
    Task<ApiResponse<CustomerProfileResponseDto>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateCustomerProfileRequestDto request);
    Task<ApiResponse<CustomerDashboardResponseDto>> GetDashboardAsync(ClaimsPrincipal principal);
    Task<ApiResponse<IReadOnlyList<CustomerAddressResponseDto>>> GetAddressesAsync(ClaimsPrincipal principal);
    Task<ApiResponse<CustomerAddressResponseDto>> CreateAddressAsync(ClaimsPrincipal principal, CustomerAddressRequestDto request);
    Task<ApiResponse<CustomerAddressResponseDto>> UpdateAddressAsync(ClaimsPrincipal principal, Guid addressId, CustomerAddressRequestDto request);
    Task<ApiResponse<bool>> DeleteAddressAsync(ClaimsPrincipal principal, Guid addressId);
    Task<ApiResponse<IReadOnlyList<CustomerReviewResponseDto>>> GetReviewsAsync(ClaimsPrincipal principal);
    Task<ApiResponse<CustomerReviewResponseDto>> SaveReviewAsync(ClaimsPrincipal principal, CustomerReviewRequestDto request);
    Task<ApiResponse<bool>> DeleteReviewAsync(ClaimsPrincipal principal, Guid reviewId);
}
