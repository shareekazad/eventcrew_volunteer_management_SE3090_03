namespace EventCrew.Api.DTOs.Shifts;

public class ShiftAssignmentResponseDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public string? ShiftTitle { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Guid VolunteerId { get; set; }
    public string? VolunteerName { get; set; }
    public string? VolunteerEmail { get; set; }
    public string Status { get; set; } = "Confirmed";
    public Guid? AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
