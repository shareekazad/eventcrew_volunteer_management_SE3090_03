namespace EventCrew.Domain.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; }
    public Guid ShiftAssignmentId { get; set; }
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public decimal? CheckInLatitude { get; set; }
    public decimal? CheckInLongitude { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal VerifiedHours { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ShiftAssignment ShiftAssignment { get; set; } = null!;
}
