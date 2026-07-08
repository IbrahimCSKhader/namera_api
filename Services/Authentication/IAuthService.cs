using System.Security.Claims;
using namera_API.Common.Responses;
using namera_API.DTOs.Authentication;

namespace namera_API.Services.Authentication;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterCustomerAsync(RegisterRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<CurrentUserDto>> GetCurrentUserAsync(ClaimsPrincipal principal);
}
