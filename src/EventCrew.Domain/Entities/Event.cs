namespace EventCrew.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}