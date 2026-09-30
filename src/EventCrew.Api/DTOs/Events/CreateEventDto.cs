using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Events;

/// <summary>
/// Request shape for creating a new event, optionally with nested role requirements.
/// </summary>
public class CreateEventDto
{
    [Required(ErrorMessage = "Organizer ID is required.")]
    public Guid OrganizerId { get; set; }

    public Guid? VenueId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Category is required.")]
    [MaxLength(50, ErrorMessage = "Category cannot exceed 50 characters.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required.")]
    public DateTimeOffset StartDate { get; set; }

    [Required(ErrorMessage = "End date is required.")]
    public DateTimeOffset EndDate { get; set; }

    /// <summary>
    /// Optional list of role requirements to create alongside the event.
    /// </summary>
    public List<RoleRequirementInputDto> RoleRequirements { get; set; } = new();
}