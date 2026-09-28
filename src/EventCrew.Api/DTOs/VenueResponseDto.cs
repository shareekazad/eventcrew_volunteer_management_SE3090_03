namespace EventCrew.Api.DTOs.Venues;

/// <summary>
/// Response shape returned to clients when reading a venue.
/// Deliberately flat — no navigation properties leak out.
/// </summary>
public class VenueResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int Capacity { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}