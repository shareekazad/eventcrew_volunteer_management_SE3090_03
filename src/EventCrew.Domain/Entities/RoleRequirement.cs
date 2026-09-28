namespace EventCrew.Domain.Entities;

public class RoleRequirement
{
    public Guid Id { get; set; }
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}