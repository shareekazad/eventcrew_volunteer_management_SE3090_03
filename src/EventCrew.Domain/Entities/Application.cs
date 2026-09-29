namespace EventCrew.Domain.Entities;

/// <summary>
/// Represents a volunteer's application to participate in a specific event.
/// Business rule: a volunteer may only have one application per event
/// (enforced by a unique constraint on (EventId, VolunteerId)).
/// </summary>
public class Application
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }
    public Guid VolunteerId { get; set; }

    /// <summary>Optional: the specific role the volunteer is applying for.</summary>
    public Guid? RoleRequirementId { get; set; }

    /// <summary>
    /// Lifecycle status of this application.
    /// Allowed values: Submitted | UnderReview | Shortlisted | Accepted | Rejected
    /// Valid transitions: Submitted → UnderReview → Shortlisted → Accepted / Rejected
    /// </summary>
    public string Status { get; set; } = "Submitted";

    /// <summary>Organizer / reviewer notes attached to the application.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp when the volunteer submitted this application.</summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>UTC timestamp when the application was last reviewed by an organizer.</summary>
    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Event Event { get; set; } = null!;
    public VolunteerProfile Volunteer { get; set; } = null!;
    public RoleRequirement? RoleRequirement { get; set; }
}
