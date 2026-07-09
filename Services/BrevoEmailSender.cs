using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LoginSystem.Services;

public sealed class BrevoEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly BrevoSettings _settings;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(HttpClient httpClient, IOptions<BrevoSettings> settings, ILogger<BrevoEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Brevo is not configured. Set Brevo:ApiKey via user-secrets or an environment variable.");

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            throw new InvalidOperationException("Brevo:FromEmail is not configured.");
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var requestBody = new
        {
            sender = new { name = _settings.FromName, email = _settings.FromEmail },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.Add("api-key", _settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            // Same reasoning as ResendEmailSender: log, don't throw. The user
            // account was already created before this method was called —
            // a failed email shouldn't undo that.
            _logger.LogError("Brevo email failed ({Status}) to {To}: {Error}", response.StatusCode, toEmail, errorBody);
        }
    }
}