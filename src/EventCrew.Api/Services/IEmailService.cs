namespace EventCrew.Api.Services;

/// <summary>
/// Contract for sending transactional emails.
/// Uses a third-party provider (Resend) under the hood.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a transactional email via the configured provider.
    /// Returns true if the send succeeded; false on failure.
    /// Never throws — logs and returns false so callers can continue.
    /// </summary>
    Task<bool> SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);

    // ---- Domain-specific helpers ----

    /// <summary>Sends an "event published" notification to the organizer.</summary>
    Task<bool> SendEventPublishedAsync(
        string toAddress,
        string eventTitle,
        DateTimeOffset startDate,
        string? venueName,
        CancellationToken cancellationToken = default);

    /// <summary>Sends an "AI plan approved" notification to the organizer.</summary>
    Task<bool> SendPlanApprovedAsync(
        string toAddress,
        string eventTitle,
        Guid workflowRunId,
        CancellationToken cancellationToken = default);
}