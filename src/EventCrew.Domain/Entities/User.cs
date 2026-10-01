using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventCrew.Domain.Entities;

/// <summary>
/// ⚠️ SHARED ENTITY — owned by the team (shared identity layer).
/// This is a MINIMAL STUB created by Student 1 only to unblock the Event entity build.
/// The owner should extend this with: password_hash, phone_number, is_active,
/// created_at, updated_at, and proper role-based authorization. DO NOT modify without coordinating.
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

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    // Reverse navigation
    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
