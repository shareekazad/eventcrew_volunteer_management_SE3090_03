using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// ⚠️ SHARED ENTITY — owned by the team (shared identity layer).
/// Contains core identity fields and reverse navigation to organized events and volunteer profiles.
/// </summary>
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("role")]
    public string Role { get; set; } = "Volunteer";

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = "hashed_default_password_dev_123";

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; } = "0771234567";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Reverse navigation - Student 1: Organized events
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

    // Reverse navigation - Student 2: Volunteer profile
    public VolunteerProfile? VolunteerProfile { get; set; }
}
