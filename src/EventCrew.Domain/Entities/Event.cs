namespace EventCrew.Domain.Entities;

/// <summary>
/// Stub entity representing an event. Owned by Student 1 (Event Management module).
/// Defined here solely so EF Core can model FK relationships from the Application entity.
/// </summary>
public class Event
{
    public Guid Id { get; set; }
}
