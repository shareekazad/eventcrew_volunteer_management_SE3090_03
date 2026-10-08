namespace EventCrew.Api.DTOs.Shifts;

/// <summary>Response DTO for a shift swap request.</summary>
public class ShiftSwapResponseDto
{
    public Guid Id { get; set; }

    // Requester side
    public Guid RequesterAssignmentId { get; set; }
    public Guid RequesterVolunteerId { get; set; }
    public string? RequesterVolunteerName { get; set; }
    public Guid RequesterShiftId { get; set; }
    public string? RequesterShiftTitle { get; set; }
    public DateTimeOffset RequesterShiftStartTime { get; set; }
    public DateTimeOffset RequesterShiftEndTime { get; set; }

    // Target side
    public Guid TargetVolunteerId { get; set; }
    public string? TargetVolunteerName { get; set; }
    public Guid TargetShiftId { get; set; }
    public string? TargetShiftTitle { get; set; }
    public DateTimeOffset TargetShiftStartTime { get; set; }
    public DateTimeOffset TargetShiftEndTime { get; set; }

    public string? Reason { get; set; }

    /// <summary>Pending_Target | Pending_Organizer | Approved | Rejected | Cancelled</summary>
    public string Status { get; set; } = "Pending_Target";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
