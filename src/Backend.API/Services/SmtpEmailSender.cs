using System.Net;
using System.Net.Mail;

namespace Backend.API.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        var email = _config.GetSection("Email");
        var host = email["Host"]!;
        var port = int.Parse(email["Port"] ?? "587");
        var user = email["User"]!;
        var password = email["Password"]!;
        var from = email["From"]!;
        var enableSsl = email.GetValue("EnableSsl", true);

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, password),
            EnableSsl = enableSsl
        };

        var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message);

        _logger.LogInformation("Email enviado a {To}", to);
    }
}
