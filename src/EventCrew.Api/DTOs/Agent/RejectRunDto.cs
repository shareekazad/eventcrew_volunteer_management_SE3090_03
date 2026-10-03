using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Input for rejecting a workflow run. A reason is mandatory — the spec
/// requires that rejection decisions are auditable.
/// </summary>
public class RejectRunDto
{
    [Required(ErrorMessage = "Rejection reason is required.")]
    [MaxLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters.")]
    public string Reason { get; set; } = string.Empty;
}