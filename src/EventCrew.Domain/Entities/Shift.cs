namespace EventCrew.Domain.Entities;

public class Shift
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid RoleRequirementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = "Scheduled";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Event Event { get; set; } = null!;
    public RoleRequirement RoleRequirement { get; set; } = null!;
    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}