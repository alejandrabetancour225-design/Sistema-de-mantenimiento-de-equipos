namespace Backend.API.Services;

/// En desarrollo, despues se terminara
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body)
    {
        _logger.LogInformation(
            "📧 [DEV] Email a {To} | Asunto: {Subject}\n{Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}
