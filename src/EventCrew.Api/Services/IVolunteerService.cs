using EventCrew.Api.DTOs;

namespace EventCrew.Api.Services;

/// <summary>
/// Contract for all volunteer profile and application business operations.
/// </summary>
public interface IVolunteerService
{
    /// <summary>
    /// Creates a new volunteer profile or updates the existing one for <paramref name="userId"/>.
    /// Returns the upserted profile as a response DTO.
    /// </summary>
    Task<VolunteerProfileResponseDto> UpsertProfileAsync(Guid userId, CreateVolunteerProfileDto dto);

    /// <summary>
    /// Retrieves the volunteer profile (with skills) for the given user ID.
    /// Returns <c>null</c> when no profile exists.
    /// </summary>
    Task<VolunteerProfileResponseDto?> GetProfileByUserIdAsync(Guid userId);

    /// <summary>
    /// Returns the volunteer profile by its own profile ID (for public lookup).
    /// Returns <c>null</c> when not found.
    /// </summary>
    Task<VolunteerProfileResponseDto?> GetProfileByIdAsync(Guid profileId);

    /// <summary>
    /// Submits a new application for <paramref name="volunteerId"/> to the event specified in <paramref name="dto"/>.
    /// Throws <see cref="InvalidOperationException"/> with HTTP 409 context if the volunteer already applied.
    /// </summary>
    Task<ApplicationResponseDto> ApplyForEventAsync(Guid volunteerId, ApplyEventDto dto);

    /// <summary>
    /// Returns the paginated list of applicants for an event, optionally filtered by <paramref name="statusFilter"/>.
    /// </summary>
    Task<IEnumerable<ApplicationResponseDto>> GetApplicantsByEventIdAsync(
        Guid eventId,
        string? statusFilter,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Updates the status of an application, enforcing the allowed state-machine transitions:
    /// Submitted → UnderReview → Shortlisted → Accepted | Rejected
    /// Throws <see cref="InvalidOperationException"/> on an illegal transition.
    /// </summary>
    Task<ApplicationResponseDto> UpdateApplicationStatusAsync(Guid applicationId, UpdateApplicationStatusDto dto);

    /// <summary>
    /// Retrieves all applications submitted by the given volunteer user ID.
    /// Includes event titles, venue names, and dates.
    /// </summary>
    Task<IEnumerable<ApplicationResponseDto>> GetApplicationsByVolunteerUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves all available skills in the system for skill builder selection.
    /// </summary>
    Task<IEnumerable<SkillDto>> GetAllSkillsAsync();
}
