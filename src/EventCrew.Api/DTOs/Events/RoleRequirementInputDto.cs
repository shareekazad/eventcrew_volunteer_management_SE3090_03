using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Events;

/// <summary>
/// Input shape for creating or updating a single role requirement
/// as part of a create/update event request.
/// </summary>
public class RoleRequirementInputDto
{
    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Required headcount must be greater than 0.")]
    public int RequiredHeadcount { get; set; }

    [Required(ErrorMessage = "Minimum experience level is required.")]
    [RegularExpression("^(Beginner|Intermediate|Advanced|Expert)$",
        ErrorMessage = "Min experience level must be one of: Beginner, Intermediate, Advanced, Expert.")]
    public string MinExperienceLevel { get; set; } = "Beginner";
}