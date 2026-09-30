namespace EventCrew.Api.DTOs;

/// <summary>
/// Read-only representation of a volunteer application.
/// Returned by POST /api/applications (201) and GET /api/applications/event/{eventId}.
/// </summary>
public class ApplicationResponseDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid VolunteerId { get; set; }
    public Guid? RoleRequirementId { get; set; }
    public string Status { get; set; } = string.Empty;

    /// <summary>Optional notes from the volunteer's application.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp of submission.</summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>UTC timestamp of last organizer review, if any.</summary>
    public DateTime? ReviewedAt { get; set; }
}
