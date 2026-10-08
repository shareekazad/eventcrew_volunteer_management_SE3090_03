namespace EventCrew.Api.DTOs.Shifts;

public class ShiftResponseDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid RoleRequirementId { get; set; }
    public string? RoleName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int Capacity { get; set; }
    public int AssignedCount { get; set; }
    public int RemainingCapacity { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
