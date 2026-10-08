using EventCrew.Api.DTOs.Shifts;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using FluentAssertions;

namespace EventCrew.Api.Tests.Services;

public class ShiftSwapServiceTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Setup helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (
        ShiftSwapService service,
        AppDbContext db,
        User organizerUser,
        VolunteerProfile vol1Profile,
        VolunteerProfile vol2Profile,
        Shift shift1,
        Shift shift2,
        ShiftAssignment assignment1,
        ShiftAssignment assignment2) SetupContext()
    {
        var db = TestDbFactory.Create();

        // Organizer
        var organizerUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Event Organizer",
            Email = "organizer@test.com",
            Role = "Organizer"
        };
        db.Users.Add(organizerUser);

        // Volunteer 1
        var vol1User = new User { Id = Guid.NewGuid(), FullName = "Volunteer One", Email = "vol1@test.com", Role = "Volunteer" };
        db.Users.Add(vol1User);
        var vol1Profile = new VolunteerProfile { Id = Guid.NewGuid(), UserId = vol1User.Id, EmergencyContact = "111" };
        db.VolunteerProfiles.Add(vol1Profile);

        // Volunteer 2
        var vol2User = new User { Id = Guid.NewGuid(), FullName = "Volunteer Two", Email = "vol2@test.com", Role = "Volunteer" };
        db.Users.Add(vol2User);
        var vol2Profile = new VolunteerProfile { Id = Guid.NewGuid(), UserId = vol2User.Id, EmergencyContact = "222" };
        db.VolunteerProfiles.Add(vol2Profile);

        // Event
        var ev = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = organizerUser.Id,
            Title = "Test Event",
            Category = "Tech",
            StartDate = DateTimeOffset.UtcNow.AddDays(5),
            EndDate = DateTimeOffset.UtcNow.AddDays(6)
        };
        db.Events.Add(ev);

        // Role
        var role = new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleName = "Support",
            RequiredHeadcount = 4
        };
        db.RoleRequirements.Add(role);

        // Shift 1 (morning)
        var shift1 = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Morning Shift",
            StartTime = ev.StartDate.AddHours(8),
            EndTime = ev.StartDate.AddHours(12),
            Capacity = 5,
            Status = "Scheduled"
        };
        db.Shifts.Add(shift1);

        // Shift 2 (afternoon)
        var shift2 = new Shift
        {
            Id = Guid.NewGuid(),
            EventId = ev.Id,
            RoleRequirementId = role.Id,
            Title = "Afternoon Shift",
            StartTime = ev.StartDate.AddHours(13),
            EndTime = ev.StartDate.AddHours(17),
            Capacity = 5,
            Status = "Scheduled"
        };
        db.Shifts.Add(shift2);

        // Assignment 1: vol1 → shift1
        var assignment1 = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shift1.Id,
            VolunteerId = vol1Profile.Id,
            Status = "Confirmed",
            AssignedByUserId = organizerUser.Id,
            AssignedAt = DateTimeOffset.UtcNow
        };
        db.ShiftAssignments.Add(assignment1);

        // Assignment 2: vol2 → shift2
        var assignment2 = new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            ShiftId = shift2.Id,
            VolunteerId = vol2Profile.Id,
            Status = "Confirmed",
            AssignedByUserId = organizerUser.Id,
            AssignedAt = DateTimeOffset.UtcNow
        };
        db.ShiftAssignments.Add(assignment2);

        db.SaveChanges();

        var service = new ShiftSwapService(db);
        return (service, db, organizerUser, vol1Profile, vol2Profile, shift1, shift2, assignment1, assignment2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CreateSwapRequestAsync Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSwapRequestAsync_WithValidData_CreatesRequest()
    {
        var (service, _, _, vol1, vol2, shift1, shift2, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id,
            Reason = "Schedule conflict"
        };

        var result = await service.CreateSwapRequestAsync(dto, vol1.UserId);

        result.Should().NotBeNull();
        result.RequesterAssignmentId.Should().Be(assignment1.Id);
        result.TargetVolunteerId.Should().Be(vol2.Id);
        result.TargetShiftId.Should().Be(shift2.Id);
        result.Status.Should().Be("Pending_Target");
        result.Reason.Should().Be("Schedule conflict");
    }

    [Fact]
    public async Task CreateSwapRequestAsync_WhenRequesterNotFound_ThrowsKeyNotFoundException()
    {
        var (service, _, _, _, vol2, _, shift2, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id
        };

        // Use a random user ID that has no volunteer profile
        var act = async () => await service.CreateSwapRequestAsync(dto, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Requester volunteer profile not found*");
    }

    [Fact]
    public async Task CreateSwapRequestAsync_WhenAssignmentDoesNotBelongToRequester_ThrowsUnauthorized()
    {
        var (service, _, _, vol1, vol2, _, shift2, _, assignment2) = SetupContext();

        // vol1 is trying to swap assignment2 (which belongs to vol2)
        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment2.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id
        };

        var act = async () => await service.CreateSwapRequestAsync(dto, vol1.UserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*only swap your own shift assignment*");
    }

    [Fact]
    public async Task CreateSwapRequestAsync_WhenTargetVolunteerDoesNotExist_ThrowsKeyNotFoundException()
    {
        var (service, _, _, vol1, _, shift1, shift2, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = Guid.NewGuid(),  // non-existent
            TargetShiftId = shift2.Id
        };

        var act = async () => await service.CreateSwapRequestAsync(dto, vol1.UserId);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Target volunteer profile not found*");
    }

    [Fact]
    public async Task CreateSwapRequestAsync_WhenSelfSwap_ThrowsInvalidOperationException()
    {
        var (service, _, _, vol1, _, shift1, _, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol1.Id,  // same as requester
            TargetShiftId = shift1.Id
        };

        var act = async () => await service.CreateSwapRequestAsync(dto, vol1.UserId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot create a swap request with yourself*");
    }

    [Fact]
    public async Task CreateSwapRequestAsync_WhenTargetHasNoAssignmentOnTargetShift_ThrowsInvalidOperationException()
    {
        var (service, _, _, vol1, vol2, shift1, _, assignment1, _) = SetupContext();

        // Shift1 is vol1's shift; vol2 has no assignment on shift1
        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift1.Id   // vol2 is NOT assigned to shift1
        };

        var act = async () => await service.CreateSwapRequestAsync(dto, vol1.UserId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Target volunteer does not have an active assignment*");
    }

    [Fact]
    public async Task CreateSwapRequestAsync_WhenDuplicatePendingRequest_ThrowsInvalidOperationException()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id
        };

        // First request succeeds
        await service.CreateSwapRequestAsync(dto, vol1.UserId);

        // Second request with same requester assignment must fail
        var act = async () => await service.CreateSwapRequestAsync(dto, vol1.UserId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pending swap request already exists*");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UpdateSwapStatusAsync Tests
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ShiftSwapResponseDto> CreatePendingSwap(
        ShiftSwapService service,
        VolunteerProfile vol1,
        VolunteerProfile vol2,
        Shift shift2,
        ShiftAssignment assignment1)
    {
        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id,
            Reason = "Test swap"
        };
        return await service.CreateSwapRequestAsync(dto, vol1.UserId);
    }

    [Fact]
    public async Task UpdateSwapStatus_TargetAccepts_MovesToPendingOrganizer()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        var result = await service.UpdateSwapStatusAsync(
            swap.Id,
            new UpdateSwapStatusDto { Status = "Pending_Organizer" },
            vol2.UserId,
            "Volunteer");

        result.Status.Should().Be("Pending_Organizer");
    }

    [Fact]
    public async Task UpdateSwapStatus_TargetDeclines_MovesToRejected()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        var result = await service.UpdateSwapStatusAsync(
            swap.Id,
            new UpdateSwapStatusDto { Status = "Rejected" },
            vol2.UserId,
            "Volunteer");

        result.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task UpdateSwapStatus_RequesterCancels_MovesToCancelled()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        var result = await service.UpdateSwapStatusAsync(
            swap.Id,
            new UpdateSwapStatusDto { Status = "Cancelled" },
            vol1.UserId,
            "Volunteer");

        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task UpdateSwapStatus_OrganizerApproves_SwapsAssignments()
    {
        var (service, db, organizerUser, vol1, vol2, shift1, shift2, assignment1, assignment2) = SetupContext();

        // Step 1: vol1 creates swap request
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        // Step 2: vol2 accepts
        await service.UpdateSwapStatusAsync(
            swap.Id,
            new UpdateSwapStatusDto { Status = "Pending_Organizer" },
            vol2.UserId,
            "Volunteer");

        // Step 3: organizer approves
        var result = await service.UpdateSwapStatusAsync(
            swap.Id,
            new UpdateSwapStatusDto { Status = "Approved" },
            organizerUser.Id,
            "Organizer");

        result.Status.Should().Be("Approved");

        // Verify assignments were swapped
        var updatedAssignment1 = await db.ShiftAssignments.FindAsync(assignment1.Id);
        var updatedAssignment2 = await db.ShiftAssignments.FindAsync(assignment2.Id);

        updatedAssignment1!.ShiftId.Should().Be(shift2.Id, "vol1 should now be on shift2 (the target shift)");
        updatedAssignment2!.ShiftId.Should().Be(shift1.Id, "vol2 should now be on shift1 (the original shift)");
    }

    [Fact]
    public async Task UpdateSwapStatus_OrganizerRejects_AfterTargetAccepted()
    {
        var (service, _, organizerUser, vol1, vol2, _, shift2, assignment1, _) = SetupContext();
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        await service.UpdateSwapStatusAsync(
            swap.Id, new UpdateSwapStatusDto { Status = "Pending_Organizer" }, vol2.UserId, "Volunteer");

        var result = await service.UpdateSwapStatusAsync(
            swap.Id, new UpdateSwapStatusDto { Status = "Rejected" }, organizerUser.Id, "Organizer");

        result.Status.Should().Be("Rejected");
    }

    [Fact]
    public async Task UpdateSwapStatus_TerminalState_ThrowsInvalidOperationException()
    {
        var (service, _, organizerUser, vol1, vol2, _, shift2, assignment1, _) = SetupContext();
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        // Requester cancels
        await service.UpdateSwapStatusAsync(
            swap.Id, new UpdateSwapStatusDto { Status = "Cancelled" }, vol1.UserId, "Volunteer");

        // Any further transition should fail
        var act = async () => await service.UpdateSwapStatusAsync(
            swap.Id, new UpdateSwapStatusDto { Status = "Pending_Organizer" }, vol2.UserId, "Volunteer");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*terminal state*");
    }

    [Fact]
    public async Task UpdateSwapStatus_WhenUnauthorizedVolunteerTriesToAccept_ThrowsUnauthorized()
    {
        var (service, db, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();
        var swap = await CreatePendingSwap(service, vol1, vol2, shift2, assignment1);

        // Third volunteer (unrelated)
        var stranger = new User { Id = Guid.NewGuid(), FullName = "Stranger", Email = "stranger@test.com", Role = "Volunteer" };
        var strangerProfile = new VolunteerProfile { Id = Guid.NewGuid(), UserId = stranger.Id, EmergencyContact = "999" };
        db.Users.Add(stranger);
        db.VolunteerProfiles.Add(strangerProfile);
        db.SaveChanges();

        var act = async () => await service.UpdateSwapStatusAsync(
            swap.Id,
            new UpdateSwapStatusDto { Status = "Pending_Organizer" },
            stranger.Id,
            "Volunteer");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Only the target volunteer or an admin*");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Query Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMySwapRequestsAsync_ReturnsSwapsWhereUserIsRequester()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id
        };
        await service.CreateSwapRequestAsync(dto, vol1.UserId);

        var mySwaps = await service.GetMySwapRequestsAsync(vol1.UserId);
        mySwaps.Should().HaveCount(1);
        mySwaps[0].RequesterVolunteerId.Should().Be(vol1.Id);
    }

    [Fact]
    public async Task GetMySwapRequestsAsync_ReturnsSwapsWhereUserIsTarget()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();

        var dto = new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id
        };
        await service.CreateSwapRequestAsync(dto, vol1.UserId);

        // vol2 is the target — should also see the request
        var targetSwaps = await service.GetMySwapRequestsAsync(vol2.UserId);
        targetSwaps.Should().HaveCount(1);
        targetSwaps[0].TargetVolunteerId.Should().Be(vol2.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsSwap()
    {
        var (service, _, _, vol1, vol2, _, shift2, assignment1, _) = SetupContext();

        var created = await service.CreateSwapRequestAsync(new CreateSwapRequestDto
        {
            RequesterAssignmentId = assignment1.Id,
            TargetVolunteerId = vol2.Id,
            TargetShiftId = shift2.Id
        }, vol1.UserId);

        var fetched = await service.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var (service, _, _, _, _, _, _, _, _) = SetupContext();
        var fetched = await service.GetByIdAsync(Guid.NewGuid());
        fetched.Should().BeNull();
    }
}
