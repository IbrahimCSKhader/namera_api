namespace namera_API.Configurations.Email;

public sealed class SmtpEmailSettings
{
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = "namera";
    public string FrontendBaseUrl { get; init; } = "http://localhost:5173";
}
