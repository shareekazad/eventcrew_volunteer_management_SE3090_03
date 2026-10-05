using System.Text.Json;
using EventCrew.Api.DTOs.Agent;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

public class ValidationService : IValidationService
{
    private static readonly string[] ActiveAssignmentStatuses = ["Proposed_By_AI", "Confirmed"];
    private static readonly JsonSerializerOptions PersistedJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;

    public ValidationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ValidationReportDto?> ValidateRosterAsync(
        Guid runId,
        ValidateRosterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .FirstOrDefaultAsync(candidate => candidate.Id == runId, cancellationToken);

        if (run is null)
            return null;

        if (run.Status != "AwaitingApproval")
            throw new InvalidOperationException(
                $"Cannot validate a roster for a run in '{run.Status}' status. Only 'AwaitingApproval' runs can be validated.");

        var eventEntity = await _db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == run.EventId, cancellationToken);

        if (eventEntity is null)
            throw new InvalidOperationException($"Event '{run.EventId}' for this workflow run does not exist.");

        var shifts = await _db.Shifts
            .AsNoTracking()
            .Where(shift => shift.EventId == run.EventId)
            .OrderBy(shift => shift.Id)
            .ToListAsync(cancellationToken);
        var shiftById = shifts.ToDictionary(shift => shift.Id);

        var roleRequirements = await _db.RoleRequirements
            .AsNoTracking()
            .Where(requirement => requirement.EventId == run.EventId)
            .OrderBy(requirement => requirement.Id)
            .ToListAsync(cancellationToken);

        var volunteerIds = request.Assignments
            .Select(assignment => assignment.VolunteerId)
            .Distinct()
            .ToArray();

        var volunteers = await _db.VolunteerProfiles
            .AsNoTracking()
            .Where(volunteer => volunteerIds.Contains(volunteer.Id))
            .Select(volunteer => volunteer.Id)
            .ToListAsync(cancellationToken);
        var existingVolunteerIds = volunteers.ToHashSet();

        var skillRows = await (
            from volunteerSkill in _db.VolunteerSkills.AsNoTracking()
            join skill in _db.Skills.AsNoTracking()
                on volunteerSkill.SkillId equals skill.Id
            where volunteerIds.Contains(volunteerSkill.VolunteerId)
            select new { volunteerSkill.VolunteerId, skill.Name }
        ).ToListAsync(cancellationToken);

        var skillsByVolunteer = skillRows
            .GroupBy(row => row.VolunteerId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => NormalizeSkill(row.Name))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

        var issues = new List<ValidationIssueDto>();
        var seenAssignments = new HashSet<(Guid ShiftId, Guid VolunteerId)>();
        var validAssignments = new List<ValidatedAssignment>();
        var requirementIds = roleRequirements.Select(requirement => requirement.Id).ToHashSet();

        foreach (var assignment in request.Assignments
                     .OrderBy(item => item.ShiftId)
                     .ThenBy(item => item.VolunteerId))
        {
            var key = (assignment.ShiftId, assignment.VolunteerId);
            if (!seenAssignments.Add(key))
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "duplicate_assignment",
                    Message = "The volunteer is listed more than once for this shift.",
                    ShiftId = assignment.ShiftId,
                    VolunteerId = assignment.VolunteerId
                });
                continue;
            }

            if (!shiftById.TryGetValue(assignment.ShiftId, out var shift))
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "shift_belongs_to_event",
                    Message = "The proposed shift does not belong to the workflow event.",
                    ShiftId = assignment.ShiftId,
                    VolunteerId = assignment.VolunteerId
                });
                continue;
            }

            if (shift.StartTime >= shift.EndTime)
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "shift_time_constraints",
                    Message = $"Shift '{shift.Title}' must start before it ends.",
                    ShiftId = shift.Id,
                    VolunteerId = assignment.VolunteerId
                });
                continue;
            }

            if (!requirementIds.Contains(shift.RoleRequirementId))
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "role_requirement_link",
                    Message = $"Shift '{shift.Title}' does not reference a role requirement for this event.",
                    ShiftId = shift.Id,
                    VolunteerId = assignment.VolunteerId
                });
                continue;
            }

            if (!existingVolunteerIds.Contains(assignment.VolunteerId))
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "volunteer_exists",
                    Message = "The proposed volunteer profile does not exist.",
                    ShiftId = shift.Id,
                    VolunteerId = assignment.VolunteerId
                });
                continue;
            }

            validAssignments.Add(new ValidatedAssignment(assignment, shift));
        }

        foreach (var shiftGroup in validAssignments.GroupBy(item => item.Shift.Id).OrderBy(group => group.Key))
        {
            var shift = shiftById[shiftGroup.Key];
            if (shift.Capacity <= 0)
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "shift_capacity",
                    Message = $"Shift '{shift.Title}' has an invalid capacity of {shift.Capacity}.",
                    ShiftId = shift.Id
                });
            }
            else if (shiftGroup.Count() > shift.Capacity)
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "shift_capacity",
                    Message = $"Shift '{shift.Title}' has {shiftGroup.Count()} proposed volunteers but its capacity is {shift.Capacity}.",
                    ShiftId = shift.Id
                });
            }
        }

        var roleRequirementById = roleRequirements.ToDictionary(requirement => requirement.Id);
        var roleCoverage = validAssignments
            .Where(item => roleRequirementById.ContainsKey(item.Shift.RoleRequirementId))
            .GroupBy(item => item.Shift.RoleRequirementId)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var requirement in roleRequirements)
        {
            var assignedCount = roleCoverage.GetValueOrDefault(requirement.Id);
            if (assignedCount < requirement.RequiredHeadcount)
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "role_requirement_coverage",
                    Message = $"Role '{requirement.RoleName}' requires {requirement.RequiredHeadcount} volunteers; {assignedCount} are proposed.",
                });
            }
        }

        foreach (var item in validAssignments)
        {
            var availableSkills = skillsByVolunteer.GetValueOrDefault(
                item.Proposal.VolunteerId,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            var missingSkills = (item.Proposal.RequiredSkills ?? new List<string>())
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Select(NormalizeSkill)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(skill => !availableSkills.Contains(skill))
                .OrderBy(skill => skill, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingSkills.Count > 0)
            {
                issues.Add(new ValidationIssueDto
                {
                    Rule = "required_skills",
                    Message = $"Volunteer {item.Proposal.VolunteerId} does not have the required skill(s): {string.Join(", ", missingSkills)}.",
                    ShiftId = item.Shift.Id,
                    VolunteerId = item.Proposal.VolunteerId
                });
            }
        }

        AddProposedOverlapIssues(validAssignments, issues);

        var externalAssignments = await (
            from assignment in _db.ShiftAssignments.AsNoTracking()
            join shift in _db.Shifts.AsNoTracking()
                on assignment.ShiftId equals shift.Id
            where shift.EventId != run.EventId
                && volunteerIds.Contains(assignment.VolunteerId)
                && ActiveAssignmentStatuses.Contains(assignment.Status)
            orderby assignment.VolunteerId, shift.StartTime, shift.Id
            select new ExistingAssignment(assignment, shift)
        ).ToListAsync(cancellationToken);

        AddExistingOverlapIssues(validAssignments, externalAssignments, issues);

        issues = issues
            .OrderBy(issue => issue.Rule, StringComparer.Ordinal)
            .ThenBy(issue => issue.ShiftId)
            .ThenBy(issue => issue.VolunteerId)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToList();

        var report = new ValidationReportDto
        {
            IsValid = issues.Count == 0,
            EventId = run.EventId,
            AssignmentsEvaluated = validAssignments.Count,
            Issues = issues
        };

        run.GeneratedRosterProposal = JsonSerializer.Serialize(request, PersistedJsonOptions);
        run.ValidationReport = JsonSerializer.Serialize(report, PersistedJsonOptions);
        run.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return report;
    }

    private static void AddProposedOverlapIssues(
        IReadOnlyList<ValidatedAssignment> assignments,
        ICollection<ValidationIssueDto> issues)
    {
        foreach (var volunteerGroup in assignments
                     .GroupBy(item => item.Proposal.VolunteerId)
                     .OrderBy(group => group.Key))
        {
            var ordered = volunteerGroup
                .OrderBy(item => item.Shift.StartTime)
                .ThenBy(item => item.Shift.EndTime)
                .ThenBy(item => item.Shift.Id)
                .ToList();

            for (var leftIndex = 0; leftIndex < ordered.Count; leftIndex++)
            {
                for (var rightIndex = leftIndex + 1; rightIndex < ordered.Count; rightIndex++)
                {
                    var left = ordered[leftIndex];
                    var right = ordered[rightIndex];
                    if (right.Shift.StartTime >= left.Shift.EndTime)
                        break;
                    if (left.Shift.Id == right.Shift.Id
                        || right.Shift.StartTime >= left.Shift.EndTime
                        || left.Shift.StartTime >= right.Shift.EndTime)
                    {
                        continue;
                    }

                    issues.Add(new ValidationIssueDto
                    {
                        Rule = "overlapping_shifts",
                        Message = $"Volunteer {volunteerGroup.Key} is assigned to overlapping shifts '{left.Shift.Title}' and '{right.Shift.Title}'.",
                        ShiftId = right.Shift.Id,
                        VolunteerId = volunteerGroup.Key
                    });
                }
            }
        }
    }

    private static void AddExistingOverlapIssues(
        IReadOnlyList<ValidatedAssignment> proposedAssignments,
        IReadOnlyList<ExistingAssignment> existingAssignments,
        ICollection<ValidationIssueDto> issues)
    {
        var existingByVolunteer = existingAssignments
            .GroupBy(item => item.Assignment.VolunteerId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var proposed in proposedAssignments
                     .OrderBy(item => item.Proposal.VolunteerId)
                     .ThenBy(item => item.Shift.StartTime)
                     .ThenBy(item => item.Shift.Id))
        {
            if (!existingByVolunteer.TryGetValue(proposed.Proposal.VolunteerId, out var existingForVolunteer))
                continue;

            foreach (var existing in existingForVolunteer)
            {
                if (proposed.Shift.StartTime >= existing.Shift.EndTime
                    || existing.Shift.StartTime >= proposed.Shift.EndTime)
                {
                    continue;
                }

                issues.Add(new ValidationIssueDto
                {
                    Rule = "overlapping_shifts",
                    Message = $"Volunteer {proposed.Proposal.VolunteerId} is assigned to proposed shift '{proposed.Shift.Title}', which overlaps existing shift '{existing.Shift.Title}'.",
                    ShiftId = proposed.Shift.Id,
                    VolunteerId = proposed.Proposal.VolunteerId
                });
            }
        }
    }

    private static string NormalizeSkill(string skill) => skill.Trim();

    private sealed record ValidatedAssignment(ProposedAssignmentDto Proposal, Shift Shift);

    private sealed record ExistingAssignment(ShiftAssignment Assignment, Shift Shift);
}
