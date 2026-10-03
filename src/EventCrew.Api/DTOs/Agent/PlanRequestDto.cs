using System.Text.Json.Serialization;

namespace EventCrew.Api.DTOs.Agent;

/// <summary>
/// Request sent to the Python AI service when asking for a staffing plan.
/// Uses snake_case to match the Python Pydantic model.
/// </summary>
public class PlanRequestDto
{
    [JsonPropertyName("event_id")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("event")]
    public PlanEventContextDto Event { get; set; } = new();

    [JsonPropertyName("venue")]
    public PlanVenueContextDto? Venue { get; set; }
}

public class PlanEventContextDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("venue_id")]
    public string? VenueId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public DateTimeOffset StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTimeOffset EndDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("role_requirements")]
    public List<PlanRoleRequirementContextDto> RoleRequirements { get; set; } = new();
}

public class PlanRoleRequirementContextDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("role_name")]
    public string RoleName { get; set; } = string.Empty;

    [JsonPropertyName("required_headcount")]
    public int RequiredHeadcount { get; set; }

    [JsonPropertyName("min_experience_level")]
    public string MinExperienceLevel { get; set; } = string.Empty;
}

public class PlanVenueContextDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public decimal? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public decimal? Longitude { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }
}