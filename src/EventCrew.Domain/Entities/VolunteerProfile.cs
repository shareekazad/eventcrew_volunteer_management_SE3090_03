using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// Volunteer profile owned by a registered user with the Volunteer role.
/// Stores availability, bio, and emergency contact information.
/// </summary>
[Table("volunteer_profiles")]
public class VolunteerProfile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to the application user (auth identity).</summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>Emergency contact information (phone or name + phone).</summary>
    [Required]
    [MaxLength(20)]
    [Column("emergency_contact")]
    public string EmergencyContact { get; set; } = string.Empty;

    /// <summary>Short biography / personal statement from the volunteer.</summary>
    [Column("bio")]
    public string? Bio { get; set; }

    /// <summary>Maximum number of hours the volunteer is willing to commit per week.</summary>
    [Column("max_hours_per_week")]
    public int MaxHoursPerWeek { get; set; } = 20;

    /// <summary>Aggregate rating score (0.00 – 5.00) computed from past event feedback.</summary>
    [Column("rating_score")]
    public decimal RatingScore { get; set; } = 5.00m;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<VolunteerSkill> VolunteerSkills { get; set; } = new List<VolunteerSkill>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}