namespace EventCrew.Api.Services;

/// <summary>
/// Third-party transactional email notification service contract (Section 11 Integration).
/// Dispatches official proof of registration and event application confirmations.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Dispatches transactional email asynchronously with resilience, secret protection, and logging fallback.
    /// </summary>
    /// <param name="toEmail">Recipient's email address.</param>
    /// <param name="volunteerName">Volunteer's full name.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="messageBody">Detailed email content/body.</param>
    Task SendConfirmationEmailAsync(string toEmail, string volunteerName, string subject, string messageBody);
}
