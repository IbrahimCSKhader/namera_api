using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using namera_API.Configurations.Email;

namespace namera_API.Services.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailSettings _settings;

    public SmtpEmailSender(IOptions<SmtpEmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.UserName) ||
            string.IsNullOrWhiteSpace(_settings.Password) ||
            string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException("SMTP email settings are not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password)
        };

        await client.SendMailAsync(message);
    }
}
