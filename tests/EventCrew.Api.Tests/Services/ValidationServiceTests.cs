using EventCrew.Api.DTOs.Agent;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using FluentAssertions;

namespace EventCrew.Api.Tests.Services;

public class ValidationServiceTests
{
    [Fact]
    public async Task ValidateRosterAsync_ReturnsPassForValidPlan()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 1);
        var skill = new Skill { Name = "First aid" };
        fixture.Db.Skills.Add(skill);
        fixture.Db.VolunteerSkills.Add(new VolunteerSkill
        {
            VolunteerId = fixture.Volunteer.Id,
            SkillId = skill.Id,
            ProficiencyLevel = "Advanced"
        });
        await fixture.Db.SaveChangesAsync();
        var request = Request(
            Assignment(fixture.Shift, fixture.Volunteer, "First aid"));

        var report = await new ValidationService(fixture.Db)
            .ValidateRosterAsync(fixture.Run.Id, request);

        report.Should().NotBeNull();
        report!.IsValid.Should().BeTrue();
        report.AssignmentsEvaluated.Should().Be(1);
        report.Issues.Should().BeEmpty();
        fixture.Db.AgentWorkflowRuns.Single().ValidationReport.Should().Contain("\"isValid\":true");
        fixture.Db.AgentWorkflowRuns.Single().GeneratedRosterProposal.Should().Contain("First aid");
    }

    [Fact]
    public async Task ValidateRosterAsync_FailsWhenVolunteersDoNotMeetRequiredHeadcount()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 2);

        var report = await new ValidationService(fixture.Db)
            .ValidateRosterAsync(fixture.Run.Id, Request());

        report.Should().NotBeNull();
        report!.IsValid.Should().BeFalse();
        report.Issues.Should().ContainSingle(issue => issue.Rule == "role_requirement_coverage")
            .Which.Message.Should().Contain("requires 2 volunteers; 0 are proposed");
    }

    [Fact]
    public async Task ValidateRosterAsync_FailsWhenVolunteerHasOverlappingShifts()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 2);
        var otherShift = fixture.AddShift(
            fixture.RoleRequirement.Id,
            "Overlapping shift",
            fixture.Shift.StartTime.AddMinutes(30),
            fixture.Shift.EndTime.AddHours(1));
        await fixture.Db.SaveChangesAsync();

        var report = await new ValidationService(fixture.Db).ValidateRosterAsync(
            fixture.Run.Id,
            Request(
                Assignment(fixture.Shift, fixture.Volunteer),
                Assignment(otherShift, fixture.Volunteer)));

        report.Should().NotBeNull();
        report!.IsValid.Should().BeFalse();
        report.Issues.Should().Contain(issue =>
            issue.Rule == "overlapping_shifts"
            && issue.Message.Contains("overlapping shifts"));
    }

    [Fact]
    public async Task ValidateRosterAsync_FailsWhenVolunteerOverlapsExistingActiveShift()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 1);
        var otherEvent = fixture.AddEvent();
        var otherRequirement = fixture.AddRoleRequirement(otherEvent.Id, 1);
        var otherShift = fixture.AddShift(
            otherRequirement.Id,
            "Existing shift",
            fixture.Shift.StartTime.AddMinutes(30),
            fixture.Shift.EndTime.AddHours(1),
            otherEvent.Id);
        fixture.Db.ShiftAssignments.Add(new ShiftAssignment
        {
            ShiftId = otherShift.Id,
            VolunteerId = fixture.Volunteer.Id,
            Status = "Confirmed"
        });
        await fixture.Db.SaveChangesAsync();

        var report = await new ValidationService(fixture.Db).ValidateRosterAsync(
            fixture.Run.Id,
            Request(Assignment(fixture.Shift, fixture.Volunteer)));

        report.Should().NotBeNull();
        report!.IsValid.Should().BeFalse();
        report.Issues.Should().Contain(issue =>
            issue.Rule == "overlapping_shifts"
            && issue.Message.Contains("existing shift"));
    }

    [Fact]
    public async Task ValidateRosterAsync_FailsWhenVolunteerDoesNotHaveRequiredSkills()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 1);

        var report = await new ValidationService(fixture.Db).ValidateRosterAsync(
            fixture.Run.Id,
            Request(Assignment(fixture.Shift, fixture.Volunteer, "First aid")));

        report.Should().NotBeNull();
        report!.IsValid.Should().BeFalse();
        report.Issues.Should().ContainSingle(issue => issue.Rule == "required_skills")
            .Which.Message.Should().Contain("First aid");
    }

    [Fact]
    public async Task ValidateRosterAsync_ReturnsMultipleDeterministicFailuresTogether()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 4);
        fixture.Shift.Capacity = 1;
        var secondVolunteer = new VolunteerProfile { UserId = Guid.NewGuid() };
        var overlappingShift = fixture.AddShift(
            fixture.RoleRequirement.Id,
            "Second shift",
            fixture.Shift.StartTime.AddMinutes(30),
            fixture.Shift.EndTime.AddHours(1));
        overlappingShift.Capacity = 1;
        fixture.Db.VolunteerProfiles.Add(secondVolunteer);
        await fixture.Db.SaveChangesAsync();

        var request = Request(
            Assignment(fixture.Shift, fixture.Volunteer, "First aid"),
            Assignment(fixture.Shift, secondVolunteer),
            Assignment(overlappingShift, fixture.Volunteer));
        var service = new ValidationService(fixture.Db);

        var firstReport = await service.ValidateRosterAsync(fixture.Run.Id, request);
        var secondReport = await service.ValidateRosterAsync(fixture.Run.Id, request);

        firstReport.Should().NotBeNull();
        firstReport!.IsValid.Should().BeFalse();
        firstReport.Issues.Select(issue => issue.Rule).Should().Contain(new[]
        {
            "overlapping_shifts",
            "required_skills",
            "role_requirement_coverage",
            "shift_capacity"
        });
        secondReport!.Issues.Should().BeEquivalentTo(firstReport.Issues, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ValidateRosterAsync_RejectsShiftFromAnotherEvent()
    {
        using var fixture = await CreateFixtureAsync(requiredHeadcount: 1);
        var otherEvent = fixture.AddEvent();
        var otherRequirement = fixture.AddRoleRequirement(otherEvent.Id, 1);
        var otherShift = fixture.AddShift(
            otherRequirement.Id,
            "Foreign shift",
            fixture.Shift.StartTime,
            fixture.Shift.EndTime,
            otherEvent.Id);
        await fixture.Db.SaveChangesAsync();

        var report = await new ValidationService(fixture.Db).ValidateRosterAsync(
            fixture.Run.Id,
            Request(Assignment(otherShift, fixture.Volunteer)));

        report.Should().NotBeNull();
        report!.IsValid.Should().BeFalse();
        report.Issues.Should().Contain(issue => issue.Rule == "shift_belongs_to_event");
    }

    private static ValidateRosterRequestDto Request(params ProposedAssignmentDto[] assignments) =>
        new() { Assignments = assignments.ToList() };

    private static ProposedAssignmentDto Assignment(
        Shift shift,
        VolunteerProfile volunteer,
        params string[] requiredSkills) => new()
    {
        ShiftId = shift.Id,
        VolunteerId = volunteer.Id,
        RequiredSkills = requiredSkills.ToList()
    };

    private static async Task<Fixture> CreateFixtureAsync(int requiredHeadcount)
    {
        var db = TestDbFactory.Create();
        var now = DateTimeOffset.UtcNow;
        var eventEntity = new Event
        {
            OrganizerId = Guid.NewGuid(),
            Title = "Validation event",
            Category = "Test",
            StartDate = now,
            EndDate = now.AddHours(8)
        };
        var requirement = new RoleRequirement
        {
            EventId = eventEntity.Id,
            RoleName = "First aid",
            RequiredHeadcount = requiredHeadcount
        };
        var shift = new Shift
        {
            EventId = eventEntity.Id,
            RoleRequirementId = requirement.Id,
            Title = "Morning shift",
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(3),
            Capacity = 5
        };
        var volunteer = new VolunteerProfile { UserId = Guid.NewGuid() };
        var run = new AgentWorkflowRun
        {
            EventId = eventEntity.Id,
            InitiatedByUserId = eventEntity.OrganizerId,
            Status = "AwaitingApproval",
            PromptObjective = "Validate proposed roster"
        };

        db.Events.Add(eventEntity);
        db.RoleRequirements.Add(requirement);
        db.Shifts.Add(shift);
        db.VolunteerProfiles.Add(volunteer);
        db.AgentWorkflowRuns.Add(run);
        await db.SaveChangesAsync();

        return new Fixture(db, eventEntity, requirement, shift, volunteer, run);
    }

    private sealed class Fixture(
        Infrastructure.Data.AppDbContext db,
        Event eventEntity,
        RoleRequirement roleRequirement,
        Shift shift,
        VolunteerProfile volunteer,
        AgentWorkflowRun run) : IDisposable
    {
        public Infrastructure.Data.AppDbContext Db { get; } = db;
        public Event Event { get; } = eventEntity;
        public RoleRequirement RoleRequirement { get; } = roleRequirement;
        public Shift Shift { get; } = shift;
        public VolunteerProfile Volunteer { get; } = volunteer;
        public AgentWorkflowRun Run { get; } = run;

        public Event AddEvent()
        {
            var otherEvent = new Event
            {
                OrganizerId = Guid.NewGuid(),
                Title = "Other event",
                Category = "Test",
                StartDate = Shift.StartTime,
                EndDate = Shift.EndTime.AddHours(2)
            };
            Db.Events.Add(otherEvent);
            return otherEvent;
        }

        public RoleRequirement AddRoleRequirement(Guid eventId, int headcount)
        {
            var requirement = new RoleRequirement
            {
                EventId = eventId,
                RoleName = "Other role",
                RequiredHeadcount = headcount
            };
            Db.RoleRequirements.Add(requirement);
            return requirement;
        }

        public Shift AddShift(
            Guid roleRequirementId,
            string title,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            Guid? eventId = null)
        {
            var addedShift = new Shift
            {
                EventId = eventId ?? Event.Id,
                RoleRequirementId = roleRequirementId,
                Title = title,
                StartTime = startTime,
                EndTime = endTime,
                Capacity = 5
            };
            Db.Shifts.Add(addedShift);
            return addedShift;
        }

        public void Dispose() => Db.Dispose();
    }
}
