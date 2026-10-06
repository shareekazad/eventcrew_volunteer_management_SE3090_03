using System.Security.Cryptography;
using System.Text;
using EventCrew.Api.DTOs.Attendance;
using EventCrew.Domain.Entities;
using EventCrew.Domain.Enums;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;

    public AttendanceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<QrTokenResponseDto?> CreateQrTokenAsync(
        CreateQrTokenRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dto.ShiftId, cancellationToken);

        if (shift is null)
            return null;

        if (dto.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("QR token expiration must be in the future.");

        var token = CreateOpaqueToken();
        _db.QrCodeTokens.Add(new QrCodeToken
        {
            ShiftId = shift.Id,
            TokenHash = HashToken(token),
            ExpiresAt = dto.ExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new QrTokenResponseDto
        {
            ShiftId = shift.Id,
            Token = token,
            ExpiresAt = dto.ExpiresAt
        };
    }

    public async Task<AttendanceResponseDto> CheckInAsync(
        CheckInRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var eventAndShift = await GetEventAndShiftAsync(
            dto.EventId,
            dto.ShiftId,
            cancellationToken);
        if (eventAndShift is null)
            throw new KeyNotFoundException("Event or shift does not exist.");

        var tokenHash = HashToken(dto.Token);
        var qrToken = await _db.QrCodeTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (qrToken is null || !qrToken.IsActive)
            throw new InvalidOperationException("QR token is invalid or inactive.");

        if (qrToken.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new InvalidOperationException("QR token has expired.");

        if (qrToken.ShiftId != dto.ShiftId)
            throw new InvalidOperationException("QR token is not valid for this shift or event.");

        var assignment = await _db.ShiftAssignments
            .FirstOrDefaultAsync(
                a => a.ShiftId == dto.ShiftId && a.VolunteerId == dto.VolunteerId,
                cancellationToken);
        if (assignment is null)
            throw new InvalidOperationException("Volunteer is not assigned to this shift.");

        if (assignment.Status != "Confirmed")
            throw new InvalidOperationException("Volunteer does not have a confirmed assignment.");

        var attendance = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.ShiftAssignmentId == assignment.Id, cancellationToken);

        if (attendance is null)
        {
            attendance = new AttendanceRecord
            {
                ShiftAssignmentId = assignment.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.AttendanceRecords.Add(attendance);
        }
        else if (attendance.Status != AttendanceStatus.Pending
                 || attendance.CheckInTime.HasValue
                 || attendance.CheckOutTime.HasValue)
        {
            throw new InvalidOperationException("Attendance is not in a state that allows check-in.");
        }

        var now = DateTimeOffset.UtcNow;
        attendance.CheckInTime = now;
        attendance.Status = AttendanceStatus.CheckedIn;
        attendance.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(attendance, assignment, eventAndShift.Value.Event, eventAndShift.Value.Shift);
    }

    public async Task<AttendanceResponseDto?> CheckOutAsync(
        Guid attendanceId,
        CheckOutRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var attendance = await _db.AttendanceRecords
            .Include(a => a.ShiftAssignment)
                .ThenInclude(a => a.Shift)
            .FirstOrDefaultAsync(a => a.Id == attendanceId, cancellationToken);

        if (attendance is null)
            return null;

        if (attendance.ShiftAssignment.VolunteerId != dto.VolunteerId)
            throw new InvalidOperationException("Volunteer does not belong to this attendance record.");

        if (attendance.Status != AttendanceStatus.CheckedIn
            || !attendance.CheckInTime.HasValue
            || attendance.CheckOutTime.HasValue)
        {
            throw new InvalidOperationException("Attendance is not in a state that allows check-out.");
        }

        var now = DateTimeOffset.UtcNow;
        if (now < attendance.CheckInTime.Value)
            throw new InvalidOperationException("Check-out time cannot be before check-in time.");

        var eventEntity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == attendance.ShiftAssignment.Shift.EventId, cancellationToken);
        if (eventEntity is null)
            throw new InvalidOperationException("Attendance references an event that does not exist.");

        var verifiedHours = Math.Round(
            (decimal)(now - attendance.CheckInTime.Value).TotalHours,
            2,
            MidpointRounding.AwayFromZero);
        if (verifiedHours > 999.99m)
            throw new InvalidOperationException("Participation duration exceeds the supported range.");

        attendance.CheckOutTime = now;
        attendance.VerifiedHours = verifiedHours;
        attendance.Status = AttendanceStatus.CheckedOut;
        attendance.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(attendance, attendance.ShiftAssignment, eventEntity, attendance.ShiftAssignment.Shift);
    }

    public async Task<IReadOnlyList<AttendanceResponseDto>?> GetAttendanceAsync(
        Guid? eventId,
        Guid? shiftId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (eventId.HasValue && !await _db.Events.AnyAsync(e => e.Id == eventId, cancellationToken))
            return null;

        if (shiftId.HasValue)
        {
            var shift = await _db.Shifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);
            if (shift is null)
                return null;
            if (eventId.HasValue && shift.EventId != eventId.Value)
                throw new InvalidOperationException("Shift does not belong to the requested event.");
        }

        AttendanceStatus? parsedStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<AttendanceStatus>(status, ignoreCase: false, out var value)
                || !Enum.IsDefined(value))
            {
                throw new InvalidOperationException("Attendance status is invalid.");
            }
            parsedStatus = value;
        }

        var query =
            from assignment in _db.ShiftAssignments.AsNoTracking()
            join shift in _db.Shifts.AsNoTracking()
                on assignment.ShiftId equals shift.Id
            join ev in _db.Events.AsNoTracking()
                on shift.EventId equals ev.Id
            join record in _db.AttendanceRecords.AsNoTracking()
                on assignment.Id equals record.ShiftAssignmentId into attendanceRecords
            from record in attendanceRecords.DefaultIfEmpty()
            where record != null || assignment.Status == "Confirmed"
            select new { Record = record, Assignment = assignment, Shift = shift, Event = ev };

        if (eventId.HasValue)
            query = query.Where(x => x.Event.Id == eventId.Value);
        if (shiftId.HasValue)
            query = query.Where(x => x.Shift.Id == shiftId.Value);
        if (parsedStatus == AttendanceStatus.Pending)
            query = query.Where(x => x.Record == null || x.Record.Status == AttendanceStatus.Pending);
        else if (parsedStatus.HasValue)
            query = query.Where(x => x.Record != null && x.Record.Status == parsedStatus.Value);

        var rows = await query
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => x.Record is null
                ? MapPendingToDto(x.Assignment, x.Event, x.Shift)
                : MapToDto(x.Record, x.Assignment, x.Event, x.Shift))
            .OrderByDescending(x => x.CheckInTime)
            .ToList();
    }

    public async Task<AttendanceStatisticsDto?> GetStatisticsAsync(
        Guid eventId,
        Guid? shiftId,
        CancellationToken cancellationToken = default)
    {
        var attendance = await GetAttendanceAsync(
            eventId,
            shiftId,
            status: null,
            cancellationToken: cancellationToken);
        if (attendance is null)
            return null;

        return new AttendanceStatisticsDto
        {
            EventId = eventId,
            ShiftId = shiftId,
            Total = attendance.Count,
            Pending = attendance.Count(a => a.Status == nameof(AttendanceStatus.Pending)),
            CheckedIn = attendance.Count(a => a.Status == nameof(AttendanceStatus.CheckedIn)),
            CheckedOut = attendance.Count(a => a.Status == nameof(AttendanceStatus.CheckedOut)),
            Absent = attendance.Count(a => a.Status == nameof(AttendanceStatus.Absent)),
            Excused = attendance.Count(a => a.Status == nameof(AttendanceStatus.Excused)),
            VerifiedHours = attendance.Sum(a => a.VerifiedHours)
        };
    }

    public async Task<IReadOnlyList<AttendanceResponseDto>?> GetVolunteerHistoryAsync(
        Guid volunteerId,
        CancellationToken cancellationToken = default)
    {
        if (!await _db.VolunteerProfiles.AnyAsync(v => v.Id == volunteerId, cancellationToken))
            return null;

        var query =
            from record in _db.AttendanceRecords.AsNoTracking()
            join assignment in _db.ShiftAssignments.AsNoTracking()
                on record.ShiftAssignmentId equals assignment.Id
            join shift in _db.Shifts.AsNoTracking()
                on assignment.ShiftId equals shift.Id
            join ev in _db.Events.AsNoTracking()
                on shift.EventId equals ev.Id
            where assignment.VolunteerId == volunteerId
            orderby record.CheckInTime descending
            select new { Record = record, Assignment = assignment, Shift = shift, Event = ev };

        var rows = await query.ToListAsync(cancellationToken);
        return rows.Select(x => MapToDto(x.Record, x.Assignment, x.Event, x.Shift)).ToList();
    }

    private async Task<(Event Event, Shift Shift)?> GetEventAndShiftAsync(
        Guid eventId,
        Guid shiftId,
        CancellationToken cancellationToken)
    {
        var eventEntity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        var shift = await _db.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);

        if (eventEntity is null || shift is null)
            return null;
        if (shift.EventId != eventId)
            throw new InvalidOperationException("Shift does not belong to the requested event.");

        return (eventEntity, shift);
    }

    private static AttendanceResponseDto MapToDto(
        AttendanceRecord record,
        ShiftAssignment assignment,
        Event eventEntity,
        Shift shift) => new()
    {
        Id = record.Id,
        VolunteerId = assignment.VolunteerId,
        EventId = eventEntity.Id,
        EventTitle = eventEntity.Title,
        ShiftId = shift.Id,
        ShiftTitle = shift.Title,
        CheckInTime = record.CheckInTime,
        CheckOutTime = record.CheckOutTime,
        Status = record.Status.ToString(),
        VerifiedHours = record.VerifiedHours,
        CreatedAt = record.CreatedAt,
        UpdatedAt = record.UpdatedAt
    };

    private static AttendanceResponseDto MapPendingToDto(
        ShiftAssignment assignment,
        Event eventEntity,
        Shift shift) => new()
    {
        VolunteerId = assignment.VolunteerId,
        EventId = eventEntity.Id,
        EventTitle = eventEntity.Title,
        ShiftId = shift.Id,
        ShiftTitle = shift.Title,
        Status = AttendanceStatus.Pending.ToString()
    };

    private static string CreateOpaqueToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
