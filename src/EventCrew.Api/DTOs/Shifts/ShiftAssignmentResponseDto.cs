namespace EventCrew.Api.DTOs.Shifts;

public class ShiftAssignmentResponseDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid VolunteerId { get; set; }
    public string? VolunteerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
}