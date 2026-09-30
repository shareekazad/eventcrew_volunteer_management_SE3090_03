using System.ComponentModel.DataAnnotations;

namespace EventCrew.Api.DTOs.Venues;

/// <summary>
/// Request shape for creating a new venue.
/// Validation here runs before the controller action — invalid payloads never hit the service.
/// </summary>
public class CreateVenueDto
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(150, ErrorMessage = "Name cannot exceed 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    [MaxLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string City { get; set; } = string.Empty;

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal? Longitude { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
    public int Capacity { get; set; }
}