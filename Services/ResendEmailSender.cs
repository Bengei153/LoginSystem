using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LoginSystem.Services;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(HttpClient httpClient, IOptions<ResendSettings> settings, ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new InvalidOperationException("Resend is not configured. Set Resend:ApiKey via user-secrets or an environment variable.");

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            throw new InvalidOperationException("Resend:FromEmail is not configured.");
    }

    /// <summary>
    /// Treats `body` as HTML — callers (like UsersController) are responsible
    /// for building a real HTML email, not just a raw text string.
    /// </summary>
    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var requestBody = new
        {
            from = $"{_settings.FromName} <{_settings.FromEmail}>",
            to = new[] { toEmail },
            subject,
            html = body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            // Deliberately doesn't throw: a failed verification email shouldn't
            // roll back the user account that was already created. Log loudly
            // instead so it's visible in your monitoring, and consider adding
            // a "resend verification email" endpoint later for this exact case.
            _logger.LogError("Resend email failed ({Status}) to {To}: {Error}", response.StatusCode, toEmail, errorBody);
        }
    }
}