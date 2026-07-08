namespace namera_API.DTOs.Authentication;

public sealed class AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public CurrentUserDto User { get; init; } = new();
}
