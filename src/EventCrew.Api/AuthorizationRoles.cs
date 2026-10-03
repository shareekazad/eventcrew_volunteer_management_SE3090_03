namespace EventCrew.Api;

public static class AuthorizationRoles
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Volunteer = "Volunteer";
    public const string AdminOrOrganizer = Admin + "," + Organizer;
    public const string All = Admin + "," + Organizer + "," + Volunteer;
}
