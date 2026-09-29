namespace EventCrew.Domain.Entities;

public class Application
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid VolunteerProfileId { get; set; }
    public Guid? RoleRequirementId { get; set; }
    public string Status { get; set; } = "Submitted";
}