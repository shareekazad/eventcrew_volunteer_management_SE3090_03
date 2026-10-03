using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Request body sent from React (via ASP.NET Core gateway) to the Python Volunteer Matching Agent.
/// React must NEVER call the Python service directly — all traffic routes through this gateway.
/// </summary>
public class MatchingRequestDto
{
    [Required]
    [JsonPropertyName("event_id")]
    public Guid EventId { get; set; }

    [Required]
    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("required_skills")]
    public List<string> RequiredSkills { get; set; } = new();

    [JsonPropertyName("min_experience_level")]
    public string MinExperienceLevel { get; set; } = "Beginner";

    [Range(1, 200)]
    [JsonPropertyName("required_headcount")]
    public int RequiredHeadcount { get; set; } = 1;
}
