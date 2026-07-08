using namera_API.DTOs.Authentication;
using namera_API.Models.Identity;

namespace namera_API.Services.Token;

public interface ITokenService
{
    AuthResponseDto CreateToken(ApplicationUser user, IReadOnlyList<string> roles);
}
