using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TravelPlanningSystem.Services;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}

public interface IEmailSender
{
    Task SendOtpAsync(string recipient, string code, string purpose, CancellationToken cancellationToken = default);
}

public sealed class GmailEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<GmailEmailSender> _logger;

    public GmailEmailSender(IOptions<EmailOptions> options, ILogger<GmailEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string recipient, string code, string purpose, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("Email credentials are not configured. Set Email:Username and Email:Password in User Secrets.");

        using var message = new MailMessage
        {
            From = new MailAddress(string.IsNullOrWhiteSpace(_options.From) ? _options.Username : _options.From),
            Subject = "Travel Planning System verification code",
            Body = $"Your {purpose} verification code is {code}. It expires in five minutes. If you did not request this code, you can ignore this email."
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Sent an OTP email for {Purpose} to a recipient.", purpose);
    }
}
