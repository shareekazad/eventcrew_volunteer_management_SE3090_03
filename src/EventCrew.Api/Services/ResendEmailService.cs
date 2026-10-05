using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EventCrew.Api.Configuration;
using Microsoft.Extensions.Options;

namespace EventCrew.Api.Services;

/// <summary>
/// Email service backed by Resend (https://resend.com).
///
/// Design:
/// - Uses Resend's HTTP API (no SMTP).
/// - Reads credentials from strongly-typed EmailSettings.
/// - Never throws: on failure, logs and returns false so callers
///   can continue their business logic.
/// - Supports a "DryRun" mode for tests / dev — logs but doesn't send.
/// </summary>
public class ResendEmailService : IEmailService
{
    private const string ResendApiUrl = "https://api.resend.com/emails";

    private readonly HttpClient _http;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly EmailSettings _settings;

    public ResendEmailService(
        HttpClient http,
        IOptions<EmailSettings> settings,
        ILogger<ResendEmailService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;

        // Only add the auth header if we have a real key
        if (!string.IsNullOrWhiteSpace(_settings.ResendApiKey))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ResendApiKey);
        }
    }

    // ============================================================
    // Send — core method
    // ============================================================
    public async Task<bool> SendAsync(
        string toAddress,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toAddress))
        {
            _logger.LogWarning("SendAsync called with empty recipient. Skipping.");
            return false;
        }

        // Dry run: log and return without hitting the network
        if (_settings.DryRun || string.IsNullOrWhiteSpace(_settings.ResendApiKey))
        {
            _logger.LogInformation(
                "[EMAIL DRY RUN] to={To} subject='{Subject}' (no API key configured)",
                toAddress, subject);
            return true;
        }

        var payload = new
        {
            from = $"{_settings.FromName} <{_settings.FromAddress}>",
            to = new[] { toAddress },
            subject,
            html = htmlBody,
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync(ResendApiUrl, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Email sent via Resend to {To}: '{Subject}'",
                    toAddress, subject);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Resend API returned {Status} for {To}: {Body}",
                (int)response.StatusCode, toAddress, body);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error sending email to {To}", toAddress);
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Timeout sending email to {To}", toAddress);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {To}", toAddress);
            return false;
        }
    }

    // ============================================================
    // Domain helper 1: Event Published
    // ============================================================
    public Task<bool> SendEventPublishedAsync(
        string toAddress,
        string eventTitle,
        DateTimeOffset startDate,
        string? venueName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[EventCrew] Your event '{eventTitle}' is now live";

        var venueLine = string.IsNullOrWhiteSpace(venueName)
            ? "<p><em>Venue: not yet assigned</em></p>"
            : $"<p><strong>Venue:</strong> {venueName}</p>";

        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 560px; margin: auto; padding: 24px;'>
                <h2 style='color: #4f46e5;'>Your event is live 🎉</h2>
                <p>Hi,</p>
                <p>Your event <strong>{eventTitle}</strong> has been published on EventCrew and is now visible to volunteers.</p>
                <div style='background: #f9fafb; padding: 16px; border-radius: 12px; margin: 20px 0;'>
                    <p><strong>Event:</strong> {eventTitle}</p>
                    <p><strong>Starts:</strong> {startDate:dd MMM yyyy HH:mm} UTC</p>
                    {venueLine}
                </div>
                <p>You can now run the AI staffing planner to generate a recommended roster.</p>
                <p style='color: #6b7280; font-size: 12px; margin-top: 24px;'>
                    — The EventCrew Team<br/>
                    This is an automated message. Please do not reply.
                </p>
            </div>";

        return SendAsync(toAddress, subject, body, cancellationToken);
    }

    // ============================================================
    // Domain helper 2: AI Plan Approved
    // ============================================================
    public Task<bool> SendPlanApprovedAsync(
        string toAddress,
        string eventTitle,
        Guid workflowRunId,
        CancellationToken cancellationToken = default)
    {
        var subject = $"[EventCrew] Staffing plan approved for '{eventTitle}'";

        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 560px; margin: auto; padding: 24px;'>
                <h2 style='color: #059669;'>Staffing plan approved ✅</h2>
                <p>Hi,</p>
                <p>The AI-generated staffing plan for <strong>{eventTitle}</strong> has been reviewed and approved.</p>
                <div style='background: #ecfdf5; padding: 16px; border-radius: 12px; margin: 20px 0; border-left: 4px solid #10b981;'>
                    <p style='margin: 0;'><strong>Event:</strong> {eventTitle}</p>
                    <p style='margin: 8px 0 0 0; font-family: monospace; font-size: 12px; color: #6b7280;'>
                        Workflow run: {workflowRunId}
                    </p>
                </div>
                <p>The approved roster will now be used for volunteer assignment and shift scheduling.</p>
                <p style='color: #6b7280; font-size: 12px; margin-top: 24px;'>
                    — The EventCrew Team<br/>
                    This is an automated message. Please do not reply.
                </p>
            </div>";

        return SendAsync(toAddress, subject, body, cancellationToken);
    }
}