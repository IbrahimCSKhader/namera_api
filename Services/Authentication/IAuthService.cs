using System.Security.Claims;
using namera_API.Common.Responses;
using namera_API.DTOs.Authentication;

namespace namera_API.Services.Authentication;

public interface IAuthService
{
    Task<ApiResponse<RegistrationResponseDto>> RegisterCustomerAsync(RegisterRequestDto request);
    Task<ApiResponse<bool>> ConfirmEmailAsync(ConfirmEmailRequestDto request);
    Task<ApiResponse<bool>> ResendEmailConfirmationAsync(ResendEmailConfirmationRequestDto request);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request);
    Task<ApiResponse<CurrentUserDto>> GetCurrentUserAsync(ClaimsPrincipal principal);
}
