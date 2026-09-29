namespace EventCrew.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ICollection<RoleRequirement> RoleRequirements { get; set; } = new List<RoleRequirement>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}