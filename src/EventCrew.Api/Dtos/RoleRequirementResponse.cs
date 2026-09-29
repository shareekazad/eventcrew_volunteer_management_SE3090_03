namespace EventCrew.Api.Dtos;

/// <summary>Role requirement fields available to organizer shift forms.</summary>
public sealed record RoleRequirementResponse(Guid Id, Guid EventId, string RoleName, int RequiredHeadcount);