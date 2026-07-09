namespace LoginSystem.Services;

public sealed class BrevoSettings
{
    public const string SectionName = "Brevo";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Must be an email address verified as a Sender in Brevo's dashboard
    /// (Settings > Senders). Unlike full domain authentication, this only
    /// requires confirming a 6-digit code sent to that inbox — no DNS access
    /// needed. Deliverability is lower than a fully authenticated domain, so
    /// plan to move to that once you own a domain.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "QuizWebApp";
}