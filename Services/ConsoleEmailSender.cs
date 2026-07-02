namespace LoginSystem.Services;

// Dev stand-in. Swap the DI registration for a real provider before production.
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;
    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string body)
    {
        _logger.LogInformation("EMAIL to {To} | {Subject}\n{Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}