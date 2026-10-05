namespace EventCrew.Api.DTOs.Attendance;

public class AttendanceResponseDto
{
    public Guid? Id { get; set; }
    public Guid VolunteerId { get; set; }
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public Guid ShiftId { get; set; }
    public string ShiftTitle { get; set; } = string.Empty;
    public DateTimeOffset? CheckInTime { get; set; }
    public DateTimeOffset? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal VerifiedHours { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
