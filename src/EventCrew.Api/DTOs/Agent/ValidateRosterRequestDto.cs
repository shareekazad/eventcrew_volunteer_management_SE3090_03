using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Agent;

public class ValidateRosterRequestDto
{
    [Required]
    public List<ProposedAssignmentDto> Assignments { get; set; } = new();
}

public class ProposedAssignmentDto
{
    [Required]
    public Guid ShiftId { get; set; }

    [Required]
    public Guid VolunteerId { get; set; }

    public List<string> RequiredSkills { get; set; } = new();
}
