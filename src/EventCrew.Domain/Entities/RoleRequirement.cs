namespace EventCrew.Domain.Entities;

/// <summary>
/// Stub entity representing a role requirement on an event. Owned by Student 1.
/// Defined here so EF Core can model the optional FK from Application.
/// </summary>
public class RoleRequirement
{
    public Guid Id { get; set; }
}
