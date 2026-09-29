namespace EventCrew.Domain.Entities;

public class RoleRequirement
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int RequiredHeadcount { get; set; }

    public Event Event { get; set; } = null!;
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    public bool BelongsToEvent(Guid eventId) => EventId == eventId;
}