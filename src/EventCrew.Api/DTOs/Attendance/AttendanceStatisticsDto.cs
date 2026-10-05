namespace EventCrew.Api.DTOs.Attendance;

public class AttendanceStatisticsDto
{
    public Guid EventId { get; set; }
    public Guid? ShiftId { get; set; }
    public int Total { get; set; }
    public int Pending { get; set; }
    public int CheckedIn { get; set; }
    public int CheckedOut { get; set; }
    public int Absent { get; set; }
    public int Excused { get; set; }
    public decimal VerifiedHours { get; set; }
}
