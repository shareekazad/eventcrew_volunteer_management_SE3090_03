namespace EventCrew.Api.DTOs.Shifts;

public class ShiftSwapResponseDto
{
    public Guid Id { get; set; }
    public Guid RequesterAssignmentId { get; set; }
    public Guid TargetVolunteerId { get; set; }
    public string? TargetVolunteerName { get; set; }
    public Guid TargetShiftId { get; set; }
    public string? TargetShiftTitle { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}