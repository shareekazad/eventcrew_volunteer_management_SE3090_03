using System.Security.Cryptography;
using System.Text;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/attendance")]
[Produces("application/json")]
public sealed class AttendanceController(EventCrewDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    public async Task<ActionResult<IReadOnlyList<AttendanceResponse>>> GetByShift([FromQuery] Guid shiftId, CancellationToken cancellationToken)
    {
        if (shiftId == Guid.Empty) return BadRequest(new ProblemDetails { Status = 400, Title = "ShiftId is required" });
        var shift = await dbContext.Shifts.AsNoTracking()
            .Where(item => item.Id == shiftId)
            .Select(item => new { item.Id, item.Event.OrganizerId })
            .SingleOrDefaultAsync(cancellationToken);
        if (shift is null || !ResourceOwnership.CanManageEvent(User, shift.OrganizerId))
            return NotFound(NotFoundProblem("Shift", shiftId));
        var results = await Project(dbContext.AttendanceRecords.AsNoTracking().Where(record => record.ShiftAssignment.ShiftId == shiftId)).OrderBy(record => record.VolunteerName).ToListAsync(cancellationToken);
        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    public async Task<ActionResult<AttendanceResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var recordQuery = dbContext.AttendanceRecords.AsNoTracking().Where(item => item.Id == id);
        if (!ResourceOwnership.IsAdmin(User))
        {
            var organizerId = ResourceOwnership.GetUserId(User);
            recordQuery = organizerId.HasValue
                ? recordQuery.Where(item => item.ShiftAssignment.Shift.Event.OrganizerId == organizerId.Value)
                : recordQuery.Where(_ => false);
        }
        var record = await Project(recordQuery).SingleOrDefaultAsync(cancellationToken);
        return record is null ? NotFound(NotFoundProblem("Attendance record", id)) : Ok(record);
    }

    [HttpPost("check-in")]
    [Authorize(Roles = AuthorizationRoles.Volunteer)]
    public async Task<ActionResult<AttendanceResponse>> CheckIn(AttendanceActionRequest request, CancellationToken cancellationToken)
    {
        var identity = await ResolveVolunteerAsync(request, cancellationToken);
        if (identity.Error is not null) return identity.Error;
        var volunteer = identity.Volunteer!;
        var assignment = await dbContext.ShiftAssignments.Include(item => item.Volunteer).ThenInclude(profile => profile.User)
            .SingleOrDefaultAsync(item => item.ShiftId == request.ShiftId && item.VolunteerId == volunteer.Id, cancellationToken);
        if (assignment is null) return NotFound(NotFoundProblem("Assignment", request.VolunteerId));
        if (assignment.Status is not ("Confirmed" or "Completed")) return BadRequest(ValidationProblem("The assignment is not valid for attendance."));
        if (assignment.Volunteer.User.Role != "Volunteer" || !assignment.Volunteer.User.IsActive) return BadRequest(ValidationProblem("The volunteer is not active."));
        var token = await ValidateToken(request, cancellationToken);
        if (token.Error is not null) return token.Error;
        var now = DateTimeOffset.UtcNow;
        var attendance = await dbContext.AttendanceRecords.SingleOrDefaultAsync(item => item.ShiftAssignmentId == assignment.Id, cancellationToken);
        if (attendance?.CheckInTime is not null) return Conflict(ConflictProblem("Volunteer has already checked in."));
        if (attendance is null)
        {
            attendance = new AttendanceRecord { Id = Guid.NewGuid(), ShiftAssignmentId = assignment.Id, CreatedAt = now };
            dbContext.AttendanceRecords.Add(attendance);
        }
        attendance.CheckInTime = now;
        attendance.Status = "CheckedIn";
        attendance.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await Project(dbContext.AttendanceRecords.AsNoTracking()).SingleAsync(item => item.Id == attendance.Id, cancellationToken));
    }

    [HttpPost("check-out")]
    [Authorize(Roles = AuthorizationRoles.Volunteer)]
    public async Task<ActionResult<AttendanceResponse>> CheckOut(AttendanceActionRequest request, CancellationToken cancellationToken)
    {
        var identity = await ResolveVolunteerAsync(request, cancellationToken);
        if (identity.Error is not null) return identity.Error;
        var volunteer = identity.Volunteer!;
        var assignment = await dbContext.ShiftAssignments.SingleOrDefaultAsync(item => item.ShiftId == request.ShiftId && item.VolunteerId == volunteer.Id, cancellationToken);
        if (assignment is null) return NotFound(NotFoundProblem("Assignment", request.VolunteerId));
        var token = await ValidateToken(request, cancellationToken);
        if (token.Error is not null) return token.Error;
        var attendance = await dbContext.AttendanceRecords.SingleOrDefaultAsync(item => item.ShiftAssignmentId == assignment.Id, cancellationToken);
        if (attendance is null) return NotFound(NotFoundProblem("Attendance record", assignment.Id));
        if (attendance.CheckInTime is null) return Conflict(ConflictProblem("Volunteer has not checked in."));
        if (attendance.CheckOutTime is not null) return Conflict(ConflictProblem("Volunteer has already checked out."));
        var now = DateTimeOffset.UtcNow;
        if (now <= attendance.CheckInTime) return Conflict(ConflictProblem("Checkout must occur after check-in."));
        attendance.CheckOutTime = now;
        attendance.Status = "CheckedOut";
        attendance.VerifiedHours = Math.Round((decimal)(now - attendance.CheckInTime.Value).TotalHours, 2, MidpointRounding.AwayFromZero);
        attendance.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await Project(dbContext.AttendanceRecords.AsNoTracking()).SingleAsync(item => item.Id == attendance.Id, cancellationToken));
    }

    internal async Task<(QrCodeToken? Token, ActionResult? Error)> ValidateToken(AttendanceActionRequest request, CancellationToken cancellationToken)
    {
        var shiftExists = await dbContext.Shifts.AnyAsync(shift => shift.Id == request.ShiftId, cancellationToken);
        if (!shiftExists) return (null, NotFound(NotFoundProblem("Shift", request.ShiftId)));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var token = await dbContext.QrCodeTokens.SingleOrDefaultAsync(item => item.TokenHash == hash, cancellationToken);
        if (token is null) return (null, NotFound(new ProblemDetails { Status = 404, Title = "QR token not found" }));
        if (!token.IsActive) return (null, Conflict(ConflictProblem("QR token is inactive.")));
        if (token.ExpiresAt <= DateTimeOffset.UtcNow) return (null, Conflict(ConflictProblem("QR token has expired.")));
        if (token.ShiftId != request.ShiftId) return (null, Conflict(ConflictProblem("QR token is not valid for this shift.")));
        return (token, null);
    }

    private async Task<(VolunteerProfile? Volunteer, ActionResult? Error)> ResolveVolunteerAsync(
        AttendanceActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
            return (null, Unauthorized());

        var volunteer = await dbContext.VolunteerProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.UserId == userId &&
                profile.User.Role == AuthorizationRoles.Volunteer && profile.User.IsActive, cancellationToken);
        if (volunteer is null)
            return (null, NotFound(new ProblemDetails { Status = 404, Title = "Assignment not found" }));

        if (request.VolunteerId != volunteer.Id)
            return (null, Forbid());

        return (volunteer, null);
    }

    internal static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private IQueryable<AttendanceResponse> Project(IQueryable<AttendanceRecord> records) => records.Select(record => new AttendanceResponse(record.Id, record.ShiftAssignmentId, record.ShiftAssignment.ShiftId, record.ShiftAssignment.Shift.Title, record.ShiftAssignment.VolunteerId, record.ShiftAssignment.Volunteer.User.FullName, record.ShiftAssignment.Volunteer.User.Email, record.Status, record.CheckInTime, record.CheckOutTime, record.VerifiedHours, record.CreatedAt, record.UpdatedAt));
    private static ProblemDetails NotFoundProblem(string resource, Guid id) => new() { Status = 404, Title = $"{resource} not found", Detail = $"No {resource.ToLowerInvariant()} exists with identifier '{id}'." };
    private static ProblemDetails ConflictProblem(string detail) => new() { Status = 409, Title = "Attendance conflict", Detail = detail };
    private static ValidationProblemDetails ValidationProblem(string detail) => new(new Dictionary<string, string[]> { ["request"] = [detail] });
}
