namespace EventCrew.Domain.Entities;

/// <summary>
/// Stub entity representing a user account. Owned by the shared authentication module.
/// Defined here solely so EF Core can model FK relationships from this layer.
/// </summary>
public class User
{
    public Guid Id { get; set; }
}
