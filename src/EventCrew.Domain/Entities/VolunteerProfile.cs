namespace EventCrew.Domain.Entities;

public class VolunteerProfile
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string FullName { get; set; } = "Demo Volunteer";
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ShiftAssignment> Assignments { get; set; } = new List<ShiftAssignment>();
}