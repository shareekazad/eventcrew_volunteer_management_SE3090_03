using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs;

/// <summary>
/// Input payload for creating or updating a volunteer profile.
/// Used by POST /api/volunteers/profile.
/// </summary>
public class CreateVolunteerProfileDto
{
    /// <summary>Emergency contact phone number or "Name: phone" format.</summary>
    [Required]
    [StringLength(20, MinimumLength = 7, ErrorMessage = "EmergencyContact must be between 7 and 20 characters.")]
    public string EmergencyContact { get; set; } = string.Empty;

    /// <summary>Optional short biography.</summary>
    [StringLength(1000)]
    public string? Bio { get; set; }

    /// <summary>Maximum hours the volunteer can commit per week. Must be positive.</summary>
    [Required]
    [Range(1, 168, ErrorMessage = "MaxHoursPerWeek must be between 1 and 168.")]
    public int MaxHoursPerWeek { get; set; } = 20;

    /// <summary>List of skill IDs the volunteer possesses (optional).</summary>
    public List<Guid> SkillIds { get; set; } = new();
}
