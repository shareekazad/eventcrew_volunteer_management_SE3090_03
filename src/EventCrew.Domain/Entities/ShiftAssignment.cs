namespace EventCrew.Domain.Entities;

public class ShiftAssignment
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid VolunteerId { get; set; }
    public string Status { get; set; } = "Confirmed";
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Shift Shift { get; set; } = null!;
    public VolunteerProfile Volunteer { get; set; } = null!;
}