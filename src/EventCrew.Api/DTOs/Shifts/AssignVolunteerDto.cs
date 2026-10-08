using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Shifts;

public class AssignVolunteerDto
{
    [Required]
    public Guid VolunteerId { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; } = "Confirmed";
}
