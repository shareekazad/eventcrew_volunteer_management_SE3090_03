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
[Authorize(Roles = AuthorizationRoles.All)]
[Route("api/shift-swaps")]
[Produces("application/json")]
public sealed class ShiftSwapController(EventCrewDbContext dbContext) : ControllerBase
{
    /// <summary>Creates a shift swap request.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> Create(
        CreateShiftSwapRequest request,
        CancellationToken cancellationToken)
    {
        var userId = ResourceOwnership.GetUserId(User);
        if (userId is null)
        {
            return Unauthorized();
        }

        var requesterProfile = await dbContext.VolunteerProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value, cancellationToken);
        if (requesterProfile is null || requesterProfile.User.Role != AuthorizationRoles.Volunteer || !requesterProfile.User.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid requester",
                Detail = "The authenticated user is not an active volunteer."
            });
        }

        var requesterAssignment = await dbContext.ShiftAssignments
            .Include(assignment => assignment.Shift)
            .SingleOrDefaultAsync(assignment => assignment.Id == request.RequesterAssignmentId, cancellationToken);
        if (requesterAssignment is null)
        {
            return NotFound(CreateNotFoundProblem("Requester assignment", request.RequesterAssignmentId));
        }

        if (requesterAssignment.VolunteerId != requesterProfile.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unauthorized assignment",
                Detail = "You can only request a swap for an assignment you own."
            });
        }

        if (requesterAssignment.Status != "Confirmed")
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid assignment status",
                Detail = "Only active confirmed assignments can be swapped."
            });
        }

        var targetVolunteer = await dbContext.VolunteerProfiles
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.Id == request.TargetVolunteerId, cancellationToken);
        if (targetVolunteer is null)
        {
            return NotFound(CreateNotFoundProblem("Target volunteer", request.TargetVolunteerId));
        }

        if (targetVolunteer.User.Role != AuthorizationRoles.Volunteer || !targetVolunteer.User.IsActive)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid target volunteer",
                Detail = "The target volunteer is not active."
            });
        }

        if (targetVolunteer.Id == requesterProfile.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid target volunteer",
                Detail = "You cannot request a swap with yourself."
            });
        }

        var targetShift = await dbContext.Shifts
            .SingleOrDefaultAsync(shift => shift.Id == request.TargetShiftId, cancellationToken);
        if (targetShift is null)
        {
            return NotFound(CreateNotFoundProblem("Target shift", request.TargetShiftId));
        }

        var now = DateTimeOffset.UtcNow;
        var swap = new ShiftSwapRequest
        {
            Id = Guid.NewGuid(),
            RequesterAssignmentId = requesterAssignment.Id,
            TargetVolunteerId = targetVolunteer.Id,
            TargetShiftId = targetShift.Id,
            Reason = request.Reason,
            Status = ShiftSwapStatus.PendingTarget,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ShiftSwapRequests.Add(swap);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProjectSwaps(dbContext.ShiftSwapRequests.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == swap.Id, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = swap.Id }, response);
    }

    /// <summary>Gets shift swap requests relevant to the caller.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ShiftSwapResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ShiftSwapResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var swapQuery = ScopeToUserAccess(dbContext.ShiftSwapRequests.AsNoTracking());

        var swaps = await ProjectSwaps(swapQuery)
            .OrderByDescending(swap => swap.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(swaps);
    }

    /// <summary>Gets a shift swap request by identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var swap = await ProjectSwaps(ScopeToUserAccess(dbContext.ShiftSwapRequests.AsNoTracking()))
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return swap is null ? NotFound(CreateNotFoundProblem("Shift swap request", id)) : Ok(swap);
    }

    /// <summary>Target volunteer accepts a swap request.</summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> Accept(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResourceOwnership.GetUserId(User);
        if (userId is null) return Unauthorized();

        var swap = await dbContext.ShiftSwapRequests
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (swap is null) return NotFound(CreateNotFoundProblem("Shift swap request", id));

        var targetProfile = await dbContext.VolunteerProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value, cancellationToken);
        if (targetProfile is null || swap.TargetVolunteerId != targetProfile.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unauthorized",
                Detail = "Only the assigned target volunteer can accept this swap request."
            });
        }

        if (swap.Status != ShiftSwapStatus.PendingTarget)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid state transition",
                Detail = $"Cannot accept a swap request in status '{swap.Status}'."
            });
        }

        swap.Status = ShiftSwapStatus.PendingOrganizer;
        swap.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProjectSwaps(dbContext.ShiftSwapRequests.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == swap.Id, cancellationToken);
        return Ok(response);
    }

    /// <summary>Target volunteer declines a swap request.</summary>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> RejectByTarget(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResourceOwnership.GetUserId(User);
        if (userId is null) return Unauthorized();

        var swap = await dbContext.ShiftSwapRequests
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (swap is null) return NotFound(CreateNotFoundProblem("Shift swap request", id));

        var targetProfile = await dbContext.VolunteerProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value, cancellationToken);
        if (targetProfile is null || swap.TargetVolunteerId != targetProfile.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unauthorized",
                Detail = "Only the assigned target volunteer can reject this swap request."
            });
        }

        if (swap.Status != ShiftSwapStatus.PendingTarget)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid state transition",
                Detail = $"Cannot reject a swap request in status '{swap.Status}'."
            });
        }

        swap.Status = ShiftSwapStatus.Rejected;
        swap.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProjectSwaps(dbContext.ShiftSwapRequests.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == swap.Id, cancellationToken);
        return Ok(response);
    }

    /// <summary>Requester cancels a swap request.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var userId = ResourceOwnership.GetUserId(User);
        if (userId is null) return Unauthorized();

        var swap = await dbContext.ShiftSwapRequests
            .Include(candidate => candidate.RequesterAssignment)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (swap is null) return NotFound(CreateNotFoundProblem("Shift swap request", id));

        var requesterProfile = await dbContext.VolunteerProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value, cancellationToken);
        if (requesterProfile is null || swap.RequesterAssignment.VolunteerId != requesterProfile.Id)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unauthorized",
                Detail = "Only the original requester can cancel this swap request."
            });
        }

        if (swap.Status != ShiftSwapStatus.PendingTarget && swap.Status != ShiftSwapStatus.PendingOrganizer)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid state transition",
                Detail = $"Cannot cancel a swap request in status '{swap.Status}'."
            });
        }

        swap.Status = ShiftSwapStatus.Cancelled;
        swap.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProjectSwaps(dbContext.ShiftSwapRequests.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == swap.Id, cancellationToken);
        return Ok(response);
    }

    /// <summary>Organizer/Admin approves a swap request and executes assignment transfer.</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var swap = await dbContext.ShiftSwapRequests
                .Include(candidate => candidate.RequesterAssignment).ThenInclude(a => a.Shift).ThenInclude(s => s.Event)
                .Include(candidate => candidate.TargetShift).ThenInclude(s => s.Event)
                .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
            if (swap is null) return NotFound(CreateNotFoundProblem("Shift swap request", id));

            if (!ResourceOwnership.CanManageEvent(User, swap.RequesterAssignment.Shift.Event.OrganizerId) &&
                !ResourceOwnership.CanManageEvent(User, swap.TargetShift.Event.OrganizerId))
            {
                return NotFound(CreateNotFoundProblem("Shift swap request", id));
            }

            if (swap.Status != ShiftSwapStatus.PendingOrganizer)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid state transition",
                    Detail = $"Cannot approve a swap request in status '{swap.Status}'."
                });
            }

            var requesterAssignment = await dbContext.ShiftAssignments
                .Include(a => a.Volunteer).ThenInclude(v => v.User)
                .SingleOrDefaultAsync(a => a.Id == swap.RequesterAssignmentId, cancellationToken);
            if (requesterAssignment is null || requesterAssignment.Status != "Confirmed")
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid assignment",
                    Detail = "The requester assignment is no longer valid."
                });
            }

            var targetVolunteer = await dbContext.VolunteerProfiles
                .Include(v => v.User)
                .SingleOrDefaultAsync(v => v.Id == swap.TargetVolunteerId, cancellationToken);
            if (targetVolunteer is null || targetVolunteer.User.Role != AuthorizationRoles.Volunteer || !targetVolunteer.User.IsActive)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid target volunteer",
                    Detail = "The target volunteer is no longer active."
                });
            }

            var targetAssignment = await dbContext.ShiftAssignments
                .SingleOrDefaultAsync(a => a.ShiftId == swap.TargetShiftId && a.VolunteerId == swap.TargetVolunteerId && a.Status == "Confirmed", cancellationToken);

            var now = DateTimeOffset.UtcNow;
            if (targetAssignment is not null)
            {
                // Swap the volunteer assignments
                var originalRequesterVolunteerId = requesterAssignment.VolunteerId;
                requesterAssignment.VolunteerId = targetVolunteer.Id;
                requesterAssignment.UpdatedAt = now;

                targetAssignment.VolunteerId = originalRequesterVolunteerId;
                targetAssignment.UpdatedAt = now;
            }
            else
            {
                // Transfer requester assignment to target volunteer
                requesterAssignment.VolunteerId = targetVolunteer.Id;
                requesterAssignment.UpdatedAt = now;
            }

            swap.Status = ShiftSwapStatus.Approved;
            swap.UpdatedAt = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = await ProjectSwaps(dbContext.ShiftSwapRequests.AsNoTracking())
                .SingleAsync(candidate => candidate.Id == swap.Id, cancellationToken);
            return Ok(response);
        }
        catch (Exception exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(CreateConflictProblem("A conflicting assignment already exists for this volunteer and shift."));
        }
        catch (Exception exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return Conflict(CreateConflictProblem("The swap state changed while processing. Refresh and try again."));
        }
    }

    /// <summary>Organizer/Admin rejects a swap request.</summary>
    [Authorize(Roles = AuthorizationRoles.AdminOrOrganizer)]
    [HttpPost("{id:guid}/organizer-reject")]
    [ProducesResponseType(typeof(ShiftSwapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ShiftSwapResponse>> RejectByOrganizer(Guid id, CancellationToken cancellationToken)
    {
        var swap = await dbContext.ShiftSwapRequests
            .Include(candidate => candidate.RequesterAssignment).ThenInclude(a => a.Shift).ThenInclude(s => s.Event)
            .Include(candidate => candidate.TargetShift).ThenInclude(s => s.Event)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (swap is null) return NotFound(CreateNotFoundProblem("Shift swap request", id));

        if (!ResourceOwnership.CanManageEvent(User, swap.RequesterAssignment.Shift.Event.OrganizerId) &&
            !ResourceOwnership.CanManageEvent(User, swap.TargetShift.Event.OrganizerId))
        {
            return NotFound(CreateNotFoundProblem("Shift swap request", id));
        }

        if (swap.Status != ShiftSwapStatus.PendingOrganizer)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid state transition",
                Detail = $"Cannot reject a swap request in status '{swap.Status}'."
            });
        }

        swap.Status = ShiftSwapStatus.Rejected;
        swap.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await ProjectSwaps(dbContext.ShiftSwapRequests.AsNoTracking())
            .SingleAsync(candidate => candidate.Id == swap.Id, cancellationToken);
        return Ok(response);
    }

    private IQueryable<ShiftSwapRequest> ScopeToUserAccess(IQueryable<ShiftSwapRequest> swaps)
    {
        if (ResourceOwnership.IsAdmin(User)) return swaps;

        var userId = ResourceOwnership.GetUserId(User);
        if (userId is null) return swaps.Where(_ => false);

        if (ResourceOwnership.IsOrganizer(User))
        {
            return swaps.Where(swap =>
                swap.RequesterAssignment.Shift.Event.OrganizerId == userId.Value ||
                swap.TargetShift.Event.OrganizerId == userId.Value);
        }

        var volunteerProfile = dbContext.VolunteerProfiles
            .Where(profile => profile.UserId == userId.Value)
            .Select(profile => profile.Id)
            .FirstOrDefault();

        if (volunteerProfile == Guid.Empty) return swaps.Where(_ => false);

        return swaps.Where(swap =>
            swap.RequesterAssignment.VolunteerId == volunteerProfile ||
            swap.TargetVolunteerId == volunteerProfile);
    }

    private static IQueryable<ShiftSwapResponse> ProjectSwaps(IQueryable<ShiftSwapRequest> swaps) =>
        swaps.Select(swap => new ShiftSwapResponse(
            swap.Id,
            swap.RequesterAssignmentId,
            swap.RequesterAssignment.VolunteerId,
            swap.RequesterAssignment.Volunteer.User.FullName,
            swap.RequesterAssignment.Volunteer.User.Email,
            swap.RequesterAssignment.ShiftId,
            swap.RequesterAssignment.Shift.Title,
            swap.RequesterAssignment.Shift.EventId,
            swap.RequesterAssignment.Shift.Event.Title,
            swap.TargetVolunteerId,
            swap.TargetVolunteer.User.FullName,
            swap.TargetVolunteer.User.Email,
            swap.TargetShiftId,
            swap.TargetShift.Title,
            swap.TargetShift.EventId,
            swap.TargetShift.Event.Title,
            swap.Reason,
            swap.Status,
            swap.CreatedAt,
            swap.UpdatedAt));

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
        Title = "Swap conflict",
        Detail = detail
    };
}
