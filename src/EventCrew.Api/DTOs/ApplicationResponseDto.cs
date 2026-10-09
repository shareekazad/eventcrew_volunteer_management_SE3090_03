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

    /// <summary>Optional event title.</summary>
    public string? EventTitle { get; set; }

    /// <summary>Optional venue name.</summary>
    public string? VenueName { get; set; }

    /// <summary>Event start time in UTC.</summary>
    public DateTime? EventStartDate { get; set; }

    /// <summary>Event end time in UTC.</summary>
    public DateTime? EventEndDate { get; set; }

    /// <summary>Optional applied role name.</summary>
    public string? RoleName { get; set; }

    /// <summary>Nested volunteer profile details including full name, email, rating, and skills.</summary>
    public VolunteerProfileResponseDto? Volunteer { get; set; }
}
