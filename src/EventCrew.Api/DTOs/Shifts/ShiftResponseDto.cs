namespace EventCrew.Api.DTOs.Shifts;

public class ShiftResponseDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid RoleRequirementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Number of volunteers already assigned to this shift.</summary>
    public int AssignedCount { get; set; }

    /// <summary>Assigned volunteers (nested).</summary>
    public List<ShiftAssignmentResponseDto> Assignments { get; set; } = new();
}