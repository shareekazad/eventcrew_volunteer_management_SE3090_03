using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs;

/// <summary>
/// Input payload for an organizer or admin updating an application's status.
/// Used by PUT /api/applications/{id}/status.
/// Valid transitions: Submitted → UnderReview → Shortlisted → Accepted | Rejected
/// </summary>
public class UpdateApplicationStatusDto
{
    /// <summary>
    /// Target status. Must be one of:
    /// Submitted, UnderReview, Shortlisted, Accepted, Rejected
    /// </summary>
    [Required]
    [RegularExpression("^(Submitted|UnderReview|Shortlisted|Accepted|Rejected)$",
        ErrorMessage = "Status must be one of: Submitted, UnderReview, Shortlisted, Accepted, Rejected.")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Optional review notes visible internally to the organizer team.</summary>
    [StringLength(2000)]
    public string? ReviewNotes { get; set; }
}
