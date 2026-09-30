using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventCrew.Domain.Enums;

namespace EventCrew.Domain.Entities;

/// <summary>
/// Represents an event created by an organizer at a venue.
/// </summary>
[Table("events")]
public class Event
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("organizer_id")]
    public Guid OrganizerId { get; set; }

    [Column("venue_id")]
    public Guid? VenueId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("category")]
    public string Category { get; set; } = string.Empty;

    [Column("start_date")]
    public DateTimeOffset StartDate { get; set; }

    [Column("end_date")]
    public DateTimeOffset EndDate { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public EventStatus Status { get; set; } = EventStatus.Draft;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizerId))]
    public User Organizer { get; set; } = null!;

    [ForeignKey(nameof(VenueId))]
    public Venue? Venue { get; set; }

    public ICollection<RoleRequirement> RoleRequirements { get; set; } = new List<RoleRequirement>();

    // Navigation property for Student 2: Volunteer Applications
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
