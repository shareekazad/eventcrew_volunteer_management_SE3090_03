using System.Data;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventCrew.Api.Controllers;

[ApiController]
[Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
[Route("api/assignments")]
[Produces("application/json")]
public sealed class AssignmentController(EventCrewDbContext dbContext) : ControllerBase
{
    private const string AssignedStatus = "Confirmed";
    private const string ApplicationAccepted = "Accepted";
    private const string ApplicationAssigned = "Assigned";

    /// <summary>Gets assignments, optionally filtered by event, shift, or volunteer profile.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftAssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ShiftAssignmentResponse>>> GetAll(
        [FromQuery] Guid? eventId,
        [FromQuery] Guid? shiftId,
        [FromQuery] Guid? volunteerId,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty || shiftId == Guid.Empty || volunteerId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid filter identifier",
                Detail = "Filter identifiers must be valid non-empty GUIDs."
            });
        }

        var assignments = ScopeToOwnedEvents(dbContext.ShiftAssignments.AsNoTracking());
        if (eventId.HasValue) assignments = assignments.Where(assignment => assignment.Shift.EventId == eventId.Value);
        if (shiftId.HasValue) assignments = assignments.Where(assignment => assignment.ShiftId == shiftId.Value);
        if (volunteerId.HasValue) assignments = assignments.Where(assignment => assignment.VolunteerId == volunteerId.Value);

        return Ok(await ProjectAssignments(assignments).OrderBy(assignment => assignment.AssignedAt).ToListAsync(cancellationToken));
    }

    /// <summary>Gets one assignment by identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShiftAssignmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftAssignmentResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await ProjectAssignments(ScopeToOwnedEvents(dbContext.ShiftAssignments.AsNoTracking()))
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return assignment is null ? NotFound(CreateNotFoundProblem("Assignment", id)) : Ok(assignment);
    }

    /// <summary>Gets eligible volunteer profiles for a shift with remaining capacity.</summary>
    [HttpGet("eligible-volunteers")]
    [ProducesResponseType(typeof(IReadOnlyList<EligibleVolunteerResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<EligibleVolunteerResponse>>> GetEligibleVolunteers(
        [FromQuery] Guid shiftId,
        CancellationToken cancellationToken)
    {
        if (shiftId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "ShiftId is required",
                Detail = "Provide a valid shiftId query parameter."
            });
        }

        var shift = await dbContext.Shifts.AsNoTracking()
            .Where(candidate => candidate.Id == shiftId)
            .Select(candidate => new { candidate.EventId, candidate.RoleRequirementId, candidate.Capacity, candidate.Event.OrganizerId })
            .SingleOrDefaultAsync(cancellationToken);
        if (shift is null) return NotFound(CreateNotFoundProblem("Shift", shiftId));
        if (!ResourceOwnership.CanManageEvent(User, shift.OrganizerId)) return NotFound(CreateNotFoundProblem("Shift", shiftId));

        var assignedCount = await CountCapacityAssignments(shiftId).CountAsync(cancellationToken);
        if (assignedCount >= shift.Capacity) return Ok(Array.Empty<EligibleVolunteerResponse>());

        var eligibleVolunteers = await dbContext.VolunteerProfiles
            .AsNoTracking()
            .Where(profile => profile.User.Role == "Volunteer" && profile.User.IsActive)
            .Where(profile => dbContext.Applications.Any(application =>
                application.VolunteerProfileId == profile.Id &&
                application.EventId == shift.EventId &&
                (application.Status == ApplicationAccepted || application.Status == ApplicationAssigned) &&
                (application.RoleRequirementId == null || application.RoleRequirementId == shift.RoleRequirementId)))
            .Where(profile => !dbContext.ShiftAssignments.Any(assignment =>
                assignment.ShiftId == shiftId && assignment.VolunteerId == profile.Id))
            .OrderBy(profile => profile.User.FullName)
            .Select(profile => new EligibleVolunteerResponse(profile.Id, profile.User.FullName, profile.User.Email))
            .ToListAsync(cancellationToken);

        return Ok(eligibleVolunteers);
    }

    /// <summary>Assigns an eligible volunteer profile to a shift.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShiftAssignmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftAssignmentResponse>> Create(
        CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var shift = await dbContext.Shifts.Include(candidate => candidate.Event)
                .SingleOrDefaultAsync(candidate => candidate.Id == request.ShiftId, cancellationToken);
            if (shift is null) return NotFound(CreateNotFoundProblem("Shift", request.ShiftId));
            if (!ResourceOwnership.CanManageEvent(User, shift.Event.OrganizerId))
                return NotFound(CreateNotFoundProblem("Shift", request.ShiftId));

            var volunteer = await dbContext.VolunteerProfiles
                .Include(profile => profile.User)
                .SingleOrDefaultAsync(profile => profile.Id == request.VolunteerId, cancellationToken);
            if (volunteer is null) return NotFound(CreateNotFoundProblem("Volunteer profile", request.VolunteerId));
            if (volunteer.User.Role != "Volunteer" || !volunteer.User.IsActive)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.VolunteerId)] = ["The selected profile is not an active volunteer."]
                }));
            }

            if (!await dbContext.Applications.AnyAsync(application =>
                application.VolunteerProfileId == volunteer.Id &&
                application.EventId == shift.EventId &&
                (application.Status == ApplicationAccepted || application.Status == ApplicationAssigned) &&
                (application.RoleRequirementId == null || application.RoleRequirementId == shift.RoleRequirementId), cancellationToken))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [nameof(request.VolunteerId)] = ["The volunteer must have an accepted application for this event and role."]
                }));
            }

            if (await dbContext.ShiftAssignments.AnyAsync(assignment =>
                assignment.ShiftId == shift.Id && assignment.VolunteerId == volunteer.Id, cancellationToken))
            {
                return Conflict(CreateConflictProblem("This volunteer is already assigned to the shift."));
            }

            var assignedCount = await CountCapacityAssignments(shift.Id).CountAsync(cancellationToken);
            if (assignedCount >= shift.Capacity)
            {
                return Conflict(CreateConflictProblem("This shift has reached its volunteer capacity."));
            }

            var now = DateTimeOffset.UtcNow;
            var assignment = new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                ShiftId = shift.Id,
                VolunteerId = volunteer.Id,
                Status = AssignedStatus,
                AssignedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.ShiftAssignments.Add(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = await ProjectAssignments(dbContext.ShiftAssignments.AsNoTracking())
                .SingleAsync(candidate => candidate.Id == assignment.Id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, response);
        }
        catch (Exception exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(CreateConflictProblem("This volunteer is already assigned to the shift."));
        }
        catch (Exception exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return Conflict(CreateConflictProblem("The shift changed while assigning the volunteer. Refresh and try again."));
        }
    }

    /// <summary>Removes a volunteer from a shift.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await dbContext.ShiftAssignments
            .Include(candidate => candidate.Shift).ThenInclude(shift => shift.Event)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (assignment is null) return NotFound(CreateNotFoundProblem("Assignment", id));
        if (!ResourceOwnership.CanManageEvent(User, assignment.Shift.Event.OrganizerId))
            return NotFound(CreateNotFoundProblem("Assignment", id));

        dbContext.ShiftAssignments.Remove(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<ShiftAssignmentResponse> ProjectAssignments(IQueryable<ShiftAssignment> assignments) =>
        assignments.Select(assignment => new ShiftAssignmentResponse(
            assignment.Id,
            assignment.ShiftId,
            assignment.Shift.Title,
            assignment.Shift.EventId,
            assignment.Shift.Event.Title,
            assignment.VolunteerId,
            assignment.Volunteer.User.FullName,
            assignment.Volunteer.User.Email,
            assignment.Status,
            assignment.AssignedAt,
            assignment.CreatedAt,
            assignment.UpdatedAt));

    private IQueryable<ShiftAssignment> ScopeToOwnedEvents(IQueryable<ShiftAssignment> assignments)
    {
        if (ResourceOwnership.IsAdmin(User)) return assignments;
        var organizerId = ResourceOwnership.GetUserId(User);
        return organizerId.HasValue
            ? assignments.Where(assignment => assignment.Shift.Event.OrganizerId == organizerId.Value)
            : assignments.Where(_ => false);
    }

    private IQueryable<ShiftAssignment> CountCapacityAssignments(Guid shiftId) =>
        dbContext.ShiftAssignments.Where(assignment =>
            assignment.ShiftId == shiftId &&
            (assignment.Status == AssignedStatus || assignment.Status == "Completed"));

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException) return postgresException;
        }

        return null;
    }

    private static ProblemDetails CreateNotFoundProblem(string resource, Guid id) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = $"{resource} not found",
        Detail = $"No {resource.ToLowerInvariant()} exists with identifier '{id}'."
    };

    private static ProblemDetails CreateConflictProblem(string detail) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Assignment conflict",
        Detail = detail
    };
}
