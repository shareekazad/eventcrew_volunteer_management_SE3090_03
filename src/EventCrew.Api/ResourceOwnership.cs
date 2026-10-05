using System.Security.Claims;

namespace EventCrew.Api;

internal static class ResourceOwnership
{
    public static bool IsAdmin(ClaimsPrincipal? user) => user?.IsInRole(AuthorizationRoles.Admin) == true;

    public static bool IsOrganizer(ClaimsPrincipal? user) => user?.IsInRole(AuthorizationRoles.Organizer) == true;

    public static Guid? GetUserId(ClaimsPrincipal? user) =>
        Guid.TryParse(user?.FindFirst("sub")?.Value, out var userId) ? userId : null;

    public static bool CanManageEvent(ClaimsPrincipal? user, Guid organizerId) =>
        IsAdmin(user) || (IsOrganizer(user) && GetUserId(user) == organizerId);
}
