namespace EventCrew.Domain.Entities;

/// <summary>
/// Volunteer profile owned by a registered user with the Volunteer role.
/// Stores availability, bio, and emergency contact information.
/// </summary>
public class VolunteerProfile
{
    public Guid Id { get; set; }

    /// <summary>FK to the application user (auth identity).</summary>
    public Guid UserId { get; set; }

    /// <summary>Emergency contact information (phone or name + phone).</summary>
    public string EmergencyContact { get; set; } = string.Empty;

    /// <summary>Short biography / personal statement from the volunteer.</summary>
    public string? Bio { get; set; }

    /// <summary>Maximum number of hours the volunteer is willing to commit per week.</summary>
    public int MaxHoursPerWeek { get; set; } = 20;

    /// <summary>Aggregate rating score (0.00 – 5.00) computed from past event feedback.</summary>
    public decimal RatingScore { get; set; } = 5.00m;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<VolunteerSkill> VolunteerSkills { get; set; } = new List<VolunteerSkill>();
    public ICollection<Application> Applications { get; set; } = new List<Application>();
}
