using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs;

/// <summary>
/// Input payload for submitting a volunteer application to an event.
/// Used by POST /api/applications.
/// </summary>
public class ApplyEventDto
{
    [Required]
    public Guid EventId { get; set; }

    /// <summary>Optional: specific role the volunteer is targeting.</summary>
    public Guid? RoleRequirementId { get; set; }

    /// <summary>Optional cover note or motivation from the volunteer.</summary>
    [StringLength(2000)]
    public string? Notes { get; set; }
}
