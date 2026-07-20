using System.Security.Claims;
using namera_API.Common.Responses;
using namera_API.DTOs.Owner;

namespace namera_API.Services.Owner;

public interface IOwnerAccountService
{
    Task<ApiResponse<OwnerProfileResponseDto>> GetProfileAsync(ClaimsPrincipal principal);
    Task<ApiResponse<OwnerProfileResponseDto>> UpdateProfileAsync(ClaimsPrincipal principal, UpdateOwnerProfileRequestDto request);
    Task<ApiResponse<bool>> ChangePasswordAsync(ClaimsPrincipal principal, ChangeOwnerPasswordRequestDto request);
    Task<ApiResponse<StoreSettingsDto>> GetSettingsAsync();
    Task<ApiResponse<StoreSettingsDto>> UpdateSettingsAsync(UpdateStoreSettingsRequestDto request);
    Task<ApiResponse<IReadOnlyList<OwnerReviewResponseDto>>> GetReviewsAsync(bool? visible);
    Task<ApiResponse<OwnerReviewResponseDto>> SetReviewVisibilityAsync(Guid reviewId, bool isVisible);
    Task<ApiResponse<bool>> DeleteReviewAsync(Guid reviewId);
}
