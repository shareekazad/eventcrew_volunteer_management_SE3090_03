using EventCrew.Api.Controllers;
using EventCrew.Api.Dtos;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Tests;

public class ShiftSwapControllerTests
{
    private (EventCrewDbContext Context, Guid OrganizerAId, Guid OrganizerBId, Guid AdminId,
        Guid VolunteerAUserId, Guid VolunteerAProfileId, Guid VolunteerBUserId, Guid VolunteerBProfileId,
        Guid VolunteerCUserId, Guid VolunteerCProfileId,
        Guid EventAId, Guid ShiftAId, Guid AssignmentAId,
        Guid EventBId, Guid ShiftBId, Guid AssignmentBId) CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<EventCrewDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new EventCrewDbContext(options);

        var organizerAId = Guid.NewGuid();
        var organizerBId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        var volAProfile = new VolunteerProfile { Id = Guid.NewGuid(), FullName = "Volunteer A", Email = "vola@test.com" };
        var volBProfile = new VolunteerProfile { Id = Guid.NewGuid(), FullName = "Volunteer B", Email = "volb@test.com" };
        var volCProfile = new VolunteerProfile { Id = Guid.NewGuid(), FullName = "Volunteer C", Email = "volc@test.com" };

        var eventA = new Event { Id = Guid.NewGuid(), OrganizerId = organizerAId, Title = "Event A" };
        var eventB = new Event { Id = Guid.NewGuid(), OrganizerId = organizerBId, Title = "Event B" };

        var reqA = new RoleRequirement { Id = Guid.NewGuid(), EventId = eventA.Id, RoleName = "Role A", RequiredHeadcount = 5 };
        var reqB = new RoleRequirement { Id = Guid.NewGuid(), EventId = eventB.Id, RoleName = "Role B", RequiredHeadcount = 5 };

        var shiftA = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = eventA.Id,
            RoleRequirementId = reqA.Id,
            Title = "Shift A",
            StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(5),
            Capacity = 2,
            Status = "Scheduled"
        };
        var shiftB = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = eventB.Id,
            RoleRequirementId = reqB.Id,
            Title = "Shift B",
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            EndTime = DateTimeOffset.UtcNow.AddHours(6),
            Capacity = 2,
            Status = "Scheduled"
        };

        var assignmentA = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shiftA.Id,
            VolunteerId = volAProfile.Id,
            Status = "Confirmed",
            AssignedAt = DateTimeOffset.UtcNow
        };
        var assignmentB = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shiftB.Id,
            VolunteerId = volBProfile.Id,
            Status = "Confirmed",
            AssignedAt = DateTimeOffset.UtcNow
        };

        context.VolunteerProfiles.AddRange(volAProfile, volBProfile, volCProfile);
        context.Events.AddRange(eventA, eventB);
        context.RoleRequirements.AddRange(reqA, reqB);
        context.Shifts.AddRange(shiftA, shiftB);
        context.ShiftAssignments.AddRange(assignmentA, assignmentB);
        context.SaveChanges();

        return (context, organizerAId, organizerBId, adminId,
            volAProfile.Id, volAProfile.Id, volBProfile.Id, volBProfile.Id, volCProfile.Id, volCProfile.Id,
            eventA.Id, shiftA.Id, assignmentA.Id,
            eventB.Id, shiftB.Id, assignmentB.Id);
    }

    [Fact]
    public async Task Test1_VolunteerCreatesSwapForOwnAssignment()
    {
        var s = CreateSeededContext();
        var controller = new ShiftSwapController(s.Context);

        var request = new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId,
            Reason = "Schedule conflict"
        };

        var result = await controller.Create(request, CancellationToken.None);
        var createdAction = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ShiftSwapResponse>(createdAction.Value);
        Assert.Equal(ShiftSwapStatus.PendingTarget, response.Status);
        Assert.Equal(s.VolunteerAProfileId, response.RequesterVolunteerId);
    }

    [Fact]
    public async Task Test2_DemoModeUsesTheSuppliedRequesterAssignment()
    {
        var s = CreateSeededContext();
        // Volunteer A tries to create swap for Volunteer B's assignment (AssignmentB)
        var controller = new ShiftSwapController(s.Context);

        var request = new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentBId,
            TargetVolunteerId = s.VolunteerCProfileId,
            TargetShiftId = s.ShiftAId
        };

        var result = await controller.Create(request, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<ShiftSwapResponse>(created.Value);
        Assert.Equal(s.VolunteerBProfileId, response.RequesterVolunteerId);
    }

    [Fact]
    public async Task Test3_InvalidTargetVolunteerIsRejected()
    {
        var s = CreateSeededContext();
        var controller = new ShiftSwapController(s.Context);

        var request = new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = Guid.NewGuid(), // non-existent
            TargetShiftId = s.ShiftBId
        };

        var result = await controller.Create(request, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Test4_InvalidTargetShiftIsRejected()
    {
        var s = CreateSeededContext();
        var controller = new ShiftSwapController(s.Context);

        var request = new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = Guid.NewGuid() // non-existent
        };

        var result = await controller.Create(request, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Test5And6_DemoModeShowsSwapRequestsWithoutIdentityScoping()
    {
        var s = CreateSeededContext();
        var createController = new ShiftSwapController(s.Context);
        var createResult = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var swapId = ((ShiftSwapResponse)((CreatedAtActionResult)createResult.Result!).Value!).Id;

        // Target (Volunteer B) can see it
        var targetController = new ShiftSwapController(s.Context);
        var targetSwaps = Assert.IsType<OkObjectResult>((await targetController.GetAll(CancellationToken.None)).Result);
        var targetList = Assert.IsAssignableFrom<IEnumerable<ShiftSwapResponse>>(targetSwaps.Value);
        Assert.Contains(targetList, item => item.Id == swapId);

        // Demo mode has no caller identity, so the same list is available to every demo role.
        var unrelatedController = new ShiftSwapController(s.Context);
        var unrelatedSwaps = Assert.IsType<OkObjectResult>((await unrelatedController.GetAll(CancellationToken.None)).Result);
        var unrelatedList = Assert.IsAssignableFrom<IEnumerable<ShiftSwapResponse>>(unrelatedSwaps.Value);
        Assert.Contains(unrelatedList, item => item.Id == swapId);

        var getByIdUnrelated = await unrelatedController.GetById(swapId, CancellationToken.None);
        Assert.IsType<OkObjectResult>(getByIdUnrelated.Result);
    }

    [Fact]
    public async Task Test7And8_TargetCanAcceptAndRejectPendingTarget()
    {
        var s = CreateSeededContext();

        // 1. Create swap request
        var createController = new ShiftSwapController(s.Context);
        var createResult = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var swapId = ((ShiftSwapResponse)((CreatedAtActionResult)createResult.Result!).Value!).Id;

        // 2. Target accepts -> Pending_Organizer
        var targetController = new ShiftSwapController(s.Context);
        var acceptResult = await targetController.Accept(swapId, CancellationToken.None);
        var acceptOk = Assert.IsType<OkObjectResult>(acceptResult.Result);
        var acceptResponse = Assert.IsType<ShiftSwapResponse>(acceptOk.Value);
        Assert.Equal(ShiftSwapStatus.PendingOrganizer, acceptResponse.Status);

        // Create second swap to test decline
        var createResult2 = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var swapId2 = ((ShiftSwapResponse)((CreatedAtActionResult)createResult2.Result!).Value!).Id;

        // 3. Target declines -> Rejected
        var rejectResult = await targetController.RejectByTarget(swapId2, CancellationToken.None);
        var rejectOk = Assert.IsType<OkObjectResult>(rejectResult.Result);
        var rejectResponse = Assert.IsType<ShiftSwapResponse>(rejectOk.Value);
        Assert.Equal(ShiftSwapStatus.Rejected, rejectResponse.Status);
    }

    [Fact]
    public async Task Test9And10_DemoModeAllowsSwapCancellationWithoutIdentity()
    {
        var s = CreateSeededContext();

        var createController = new ShiftSwapController(s.Context);
        var createResult = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var swapId = ((ShiftSwapResponse)((CreatedAtActionResult)createResult.Result!).Value!).Id;

        // In demo mode cancellation is not tied to a signed-in volunteer.
        var targetController = new ShiftSwapController(s.Context);
        var targetCancelResult = await targetController.Cancel(swapId, CancellationToken.None);
        var targetCancelResponse = Assert.IsType<ShiftSwapResponse>(Assert.IsType<OkObjectResult>(targetCancelResult.Result).Value);
        Assert.Equal(ShiftSwapStatus.Cancelled, targetCancelResponse.Status);

        var secondCreate = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var secondSwapId = ((ShiftSwapResponse)((CreatedAtActionResult)secondCreate.Result!).Value!).Id;
        var cancelResult = await createController.Cancel(secondSwapId, CancellationToken.None);
        var cancelOk = Assert.IsType<OkObjectResult>(cancelResult.Result);
        var cancelResponse = Assert.IsType<ShiftSwapResponse>(cancelOk.Value);
        Assert.Equal(ShiftSwapStatus.Cancelled, cancelResponse.Status);
    }

    [Fact]
    public async Task Test11And12_DemoModeDoesNotScopeSwapsToOrganizerIdentity()
    {
        var s = CreateSeededContext();

        var createController = new ShiftSwapController(s.Context);
        var createResult = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftAId
        }, CancellationToken.None);
        var swapId = ((ShiftSwapResponse)((CreatedAtActionResult)createResult.Result!).Value!).Id;

        // Organizer A (owns Event A/Shift A) can see it
        var orgAController = new ShiftSwapController(s.Context);
        var orgASwaps = Assert.IsType<OkObjectResult>((await orgAController.GetAll(CancellationToken.None)).Result);
        var listA = Assert.IsAssignableFrom<IEnumerable<ShiftSwapResponse>>(orgASwaps.Value);
        Assert.Contains(listA, item => item.Id == swapId);

        // All demo roles can see swap records because no backend identity is required.
        var orgBController = new ShiftSwapController(s.Context);
        var orgBSwaps = Assert.IsType<OkObjectResult>((await orgBController.GetAll(CancellationToken.None)).Result);
        var listB = Assert.IsAssignableFrom<IEnumerable<ShiftSwapResponse>>(orgBSwaps.Value);
        Assert.Contains(listB, item => item.Id == swapId);
    }

    [Fact]
    public async Task Test13_14_15_17_18_OrganizerApproveRejectAndAdminCrossEvent()
    {
        var s = CreateSeededContext();

        // Setup swap request
        var createController = new ShiftSwapController(s.Context);
        var createResult = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var swapId = ((ShiftSwapResponse)((CreatedAtActionResult)createResult.Result!).Value!).Id;

        // Target accepts
        var targetController = new ShiftSwapController(s.Context);
        await targetController.Accept(swapId, CancellationToken.None);

        // Organizer A approves -> Approved and assignments are transferred
        var orgAController = new ShiftSwapController(s.Context);
        var approveResult = await orgAController.Approve(swapId, CancellationToken.None);
        var approveOk = Assert.IsType<OkObjectResult>(approveResult.Result);
        var approveResponse = Assert.IsType<ShiftSwapResponse>(approveOk.Value);
        Assert.Equal(ShiftSwapStatus.Approved, approveResponse.Status);

        // Verify assignment state after approved swap
        var assignmentAAfter = await s.Context.ShiftAssignments.SingleAsync(a => a.Id == s.AssignmentAId);
        var assignmentBAfter = await s.Context.ShiftAssignments.SingleAsync(a => a.Id == s.AssignmentBId);

        Assert.Equal(s.VolunteerBProfileId, assignmentAAfter.VolunteerId);
        Assert.Equal(s.VolunteerAProfileId, assignmentBAfter.VolunteerId);

        // Test Reject scenario on second swap
        var createControllerB = new ShiftSwapController(s.Context);
        var createResult2 = await createControllerB.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId, // Now held by Vol B
            TargetVolunteerId = s.VolunteerCProfileId,
            TargetShiftId = s.ShiftAId
        }, CancellationToken.None);
        var swapId2 = ((ShiftSwapResponse)((CreatedAtActionResult)createResult2.Result!).Value!).Id;

        var targetCController = new ShiftSwapController(s.Context);
        await targetCController.Accept(swapId2, CancellationToken.None);

        // Admin rejects -> Rejected, assignments unchanged
        var adminController = new ShiftSwapController(s.Context);
        var rejectResult = await adminController.RejectByOrganizer(swapId2, CancellationToken.None);
        var rejectOk = Assert.IsType<OkObjectResult>(rejectResult.Result);
        Assert.Equal(ShiftSwapStatus.Rejected, ((ShiftSwapResponse)rejectOk.Value!).Status);
    }

    [Fact]
    public async Task Test16_InvalidStatusTransitionsAreRejected()
    {
        var s = CreateSeededContext();

        var createController = new ShiftSwapController(s.Context);
        var createResult = await createController.Create(new CreateShiftSwapRequest
        {
            RequesterAssignmentId = s.AssignmentAId,
            TargetVolunteerId = s.VolunteerBProfileId,
            TargetShiftId = s.ShiftBId
        }, CancellationToken.None);
        var swapId = ((ShiftSwapResponse)((CreatedAtActionResult)createResult.Result!).Value!).Id;

        // Organizer tries to approve while still in Pending_Target -> Rejected
        var orgAController = new ShiftSwapController(s.Context);
        var directApproveResult = await orgAController.Approve(swapId, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(directApproveResult.Result);

        // Target accepts -> Pending_Organizer
        var targetController = new ShiftSwapController(s.Context);
        await targetController.Accept(swapId, CancellationToken.None);

        // Approve -> Approved
        await orgAController.Approve(swapId, CancellationToken.None);

        // Try accepting after Approved -> Rejected
        var acceptAfterApproved = await targetController.Accept(swapId, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(acceptAfterApproved.Result);
    }
}
