namespace LoginSystem.Services;

public sealed class ResendSettings
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Must be an address on a domain you've verified in Resend's dashboard.
    /// Until you verify a domain, use their sandbox sender for testing:
    /// "onboarding@resend.dev" — it only delivers to your own account email
    /// though, not real students, so treat it as dev-only.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "QuizWebApp";
}