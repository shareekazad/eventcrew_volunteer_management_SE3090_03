namespace EventCrew.Api.DTOs.Events;

/// <summary>
/// Response shape returned to clients when reading an event.
/// Includes nested role requirements, but no navigation to identity or venue entities.
/// </summary>
public class EventResponseDto
{
    public Guid Id { get; set; }
    public Guid OrganizerId { get; set; }
    public Guid? VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Nested: role requirements belong to this event
    public List<RoleRequirementDto> RoleRequirements { get; set; } = new();
}