namespace EventCrew.Api.Configuration;

/// <summary>
/// Strongly-typed configuration for the email service.
/// Bound from the "Email" section in appsettings.json / env vars.
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Resend API key. NEVER commit the real value.
    /// Set via environment variable EMAIL__RESEND_APIKEY in production.
    /// </summary>
    public string ResendApiKey { get; set; } = string.Empty;

    /// <summary>
    /// "From" email address. Use onboarding@resend.dev for sandbox,
    /// or your verified domain in production.
    /// </summary>
    public string FromAddress { get; set; } = "onboarding@resend.dev";

    /// <summary>
    /// Friendly sender name.
    /// </summary>
    public string FromName { get; set; } = "EventCrew";

    /// <summary>
    /// When true, emails are logged but not sent (safe for tests/dev).
    /// </summary>
    public bool DryRun { get; set; } = false;
}