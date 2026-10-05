namespace EventCrew.Api.DTOs;

/// <summary>
/// Full public representation of a volunteer profile, including their skill list.
/// Returned by GET /api/volunteers/me and GET /api/volunteers/{id}.
/// </summary>
public class VolunteerProfileResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string EmergencyContact { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int MaxHoursPerWeek { get; set; }
    public decimal RatingScore { get; set; }
    public List<SkillDto> Skills { get; set; } = new();
}
