using EventCrew.Api.DTOs.Agent;

namespace EventCrew.Api.Services;

public interface IValidationService
{
    Task<ValidationReportDto?> ValidateRosterAsync(
        Guid runId,
        ValidateRosterRequestDto request,
        CancellationToken cancellationToken = default);
}
