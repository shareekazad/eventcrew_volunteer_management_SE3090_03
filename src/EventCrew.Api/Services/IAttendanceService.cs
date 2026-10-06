using EventCrew.Api.DTOs.Attendance;

namespace EventCrew.Api.Services;

public interface IAttendanceService
{
    Task<QrTokenResponseDto?> CreateQrTokenAsync(
        CreateQrTokenRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<AttendanceResponseDto> CheckInAsync(
        CheckInRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<AttendanceResponseDto?> CheckOutAsync(
        Guid attendanceId,
        CheckOutRequestDto dto,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceResponseDto>?> GetAttendanceAsync(
        Guid? eventId,
        Guid? shiftId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<AttendanceStatisticsDto?> GetStatisticsAsync(
        Guid eventId,
        Guid? shiftId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceResponseDto>?> GetVolunteerHistoryAsync(
        Guid volunteerId,
        CancellationToken cancellationToken = default);
}
