using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using EventCrew.Api;
using EventCrew.Api.Controllers;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using EventCrew.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EventCrew.Api.Tests;

public sealed class AuthorizationIntegrationTests
{
    [Fact]
    public async Task ProtectedShiftEndpointReturns401WithoutAuthentication()
    {
        await using var host = await CreateHostAsync();
        var response = await host.Client.GetAsync("/api/shifts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(AuthorizationRoles.Volunteer, HttpStatusCode.OK)]
    [InlineData(AuthorizationRoles.Organizer, HttpStatusCode.OK)]
    [InlineData(AuthorizationRoles.Admin, HttpStatusCode.OK)]
    public async Task ShiftReadAllowsAllDefinedRoles(string role, HttpStatusCode expectedStatus)
    {
        await using var host = await CreateHostAsync();
        using var request = CreateRequest(HttpMethod.Get, "/api/shifts", role);

        var response = await host.Client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task VolunteerCannotCreateShiftButOrganizerAndAdminReachBusinessValidation()
    {
        await using var host = await CreateHostAsync();

        using var volunteerRequest = CreateRequest(HttpMethod.Post, "/api/shifts", AuthorizationRoles.Volunteer, "{}");
        var volunteerResponse = await host.Client.SendAsync(volunteerRequest);
        Assert.Equal(HttpStatusCode.Forbidden, volunteerResponse.StatusCode);

        foreach (var role in new[] { AuthorizationRoles.Organizer, AuthorizationRoles.Admin })
        {
            using var request = CreateRequest(HttpMethod.Post, "/api/shifts", role, "{}");
            var response = await host.Client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task AssignmentManagementIsRestrictedToOrganizerAndAdmin()
    {
        await using var host = await CreateHostAsync();

        using var volunteerRequest = CreateRequest(HttpMethod.Get, "/api/assignments", AuthorizationRoles.Volunteer);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.SendAsync(volunteerRequest)).StatusCode);

        foreach (var role in new[] { AuthorizationRoles.Organizer, AuthorizationRoles.Admin })
        {
            using var request = CreateRequest(HttpMethod.Get, "/api/assignments", role);
            Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(request)).StatusCode);
        }
    }

    [Fact]
    public async Task AttendanceCheckInRequiresVolunteerRole()
    {
        await using var host = await CreateHostAsync();

        using var organizerRequest = CreateRequest(HttpMethod.Post, "/api/attendance/check-in", AuthorizationRoles.Organizer, "{}");
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.SendAsync(organizerRequest)).StatusCode);

        using var anonymousRequest = new HttpRequestMessage(HttpMethod.Post, "/api/attendance/check-in")
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.SendAsync(anonymousRequest)).StatusCode);

        using var volunteerRequest = CreateRequest(HttpMethod.Post, "/api/attendance/check-in", AuthorizationRoles.Volunteer, "{}");
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.SendAsync(volunteerRequest)).StatusCode);
    }

    [Fact]
    public async Task VolunteerAttendanceUsesAuthenticatedProfileAndRejectsIdentitySpoofing()
    {
        await using var host = await CreateHostAsync();
        var data = await SeedResourcesAsync(host);
        using (var scope = host.App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventCrewDbContext>();
            Assert.Equal(2, await db.VolunteerProfiles.CountAsync());
            Assert.True(await db.VolunteerProfiles.AnyAsync(profile => profile.UserId == data.VolunteerAUserId));
            Assert.True(await db.Users.AnyAsync(user => user.Id == data.VolunteerAUserId && user.IsActive && user.Role == AuthorizationRoles.Volunteer));
        }
        var token = "integration-qr-token";
        await AddQrTokenAsync(host, data.ShiftAId, token);

        using var spoofedCheckIn = AttendanceRequest("/api/attendance/check-in", data, data.VolunteerBProfileId, token, data.VolunteerAUserId);
        var spoofedResponse = await host.Client.SendAsync(spoofedCheckIn);
        Assert.True(spoofedResponse.StatusCode == HttpStatusCode.Forbidden,
            $"Expected forbidden, got {(int)spoofedResponse.StatusCode}: {await spoofedResponse.Content.ReadAsStringAsync()}");
        Assert.Equal(0, await AttendanceCountAsync(host));

        using var checkIn = AttendanceRequest("/api/attendance/check-in", data, data.VolunteerAProfileId, token, data.VolunteerAUserId);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(checkIn)).StatusCode);

        using var spoofedCheckOut = AttendanceRequest("/api/attendance/check-out", data, data.VolunteerBProfileId, token, data.VolunteerAUserId);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.SendAsync(spoofedCheckOut)).StatusCode);
        Assert.Equal(1, await AttendanceCountAsync(host));

        using var checkOut = AttendanceRequest("/api/attendance/check-out", data, data.VolunteerAProfileId, token, data.VolunteerAUserId);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(checkOut)).StatusCode);
    }

    [Fact]
    public async Task OrganizerOwnershipScopesEventsShiftsAssignmentsQrAndAttendance()
    {
        await using var host = await CreateHostAsync();
        var data = await SeedResourcesAsync(host);

        using var ownEvent = CreateRequest(HttpMethod.Put, $"/api/events/{data.EventAId}", AuthorizationRoles.Organizer, data.OrganizerAId,
            EventUpdateJson("Organizer A event updated"));
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(ownEvent)).StatusCode);

        using var ownEventRead = CreateRequest(HttpMethod.Get, $"/api/events/{data.EventAId}", AuthorizationRoles.Organizer, data.OrganizerAId);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(ownEventRead)).StatusCode);

        using var foreignEvent = CreateRequest(HttpMethod.Put, $"/api/events/{data.EventBId}", AuthorizationRoles.Organizer, data.OrganizerAId,
            EventUpdateJson("Unauthorized update"));
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreignEvent)).StatusCode);

        using var foreignEventRead = CreateRequest(HttpMethod.Get, $"/api/events/{data.EventAId}", AuthorizationRoles.Organizer, data.OrganizerBId);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreignEventRead)).StatusCode);

        using var foreignShift = CreateRequest(HttpMethod.Put, $"/api/shifts/{data.ShiftAId}", AuthorizationRoles.Organizer, data.OrganizerBId,
            ShiftUpdateJson(data.EventAId, data.RoleRequirementAId));
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreignShift)).StatusCode);

        using var shiftMove = CreateRequest(HttpMethod.Put, $"/api/shifts/{data.ShiftAId}", AuthorizationRoles.Organizer, data.OrganizerAId,
            ShiftUpdateJson(data.EventBId, data.RoleRequirementBId));
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(shiftMove)).StatusCode);

        using var foreignAssignment = CreateRequest(HttpMethod.Post, "/api/assignments", AuthorizationRoles.Organizer, data.OrganizerBId,
            $$"""{"shiftId":"{{data.ShiftAId}}","volunteerId":"{{data.VolunteerBProfileId}}"}""");
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreignAssignment)).StatusCode);

        using var foreignQr = CreateRequest(HttpMethod.Post, $"/api/shifts/{data.ShiftAId}/qr-tokens", AuthorizationRoles.Organizer, data.OrganizerBId);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreignQr)).StatusCode);

        using var foreignAttendance = CreateRequest(HttpMethod.Get, $"/api/attendance?shiftId={data.ShiftAId}", AuthorizationRoles.Organizer, data.OrganizerBId);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreignAttendance)).StatusCode);

        using var ownShift = CreateRequest(HttpMethod.Put, $"/api/shifts/{data.ShiftAId}", AuthorizationRoles.Organizer, data.OrganizerAId,
            ShiftUpdateJson(data.EventAId, data.RoleRequirementAId));
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.SendAsync(ownShift)).StatusCode);

        using var ownShiftRead = CreateRequest(HttpMethod.Get, "/api/shifts", AuthorizationRoles.Organizer, data.OrganizerAId);
        var shifts = await (await host.Client.SendAsync(ownShiftRead)).Content.ReadFromJsonAsync<List<ShiftResponseBody>>();
        Assert.NotNull(shifts);
        Assert.Contains(shifts!, shift => shift.Id == data.ShiftAId);
        Assert.DoesNotContain(shifts!, shift => shift.Id == data.ShiftBId);

        using var ownAssignments = CreateRequest(HttpMethod.Get, $"/api/assignments?eventId={data.EventAId}", AuthorizationRoles.Organizer, data.OrganizerAId);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(ownAssignments)).StatusCode);

        using var ownQr = CreateRequest(HttpMethod.Post, $"/api/shifts/{data.ShiftAId}/qr-tokens", AuthorizationRoles.Organizer, data.OrganizerAId);
        Assert.Equal(HttpStatusCode.Created, (await host.Client.SendAsync(ownQr)).StatusCode);

        using var adminCrossEvent = CreateRequest(HttpMethod.Put, $"/api/events/{data.EventBId}", AuthorizationRoles.Admin, data.AdminUserId,
            EventUpdateJson("Admin updated event B"));
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(adminCrossEvent)).StatusCode);

        using var adminCrossQr = CreateRequest(HttpMethod.Post, $"/api/shifts/{data.ShiftBId}/qr-tokens", AuthorizationRoles.Admin, data.AdminUserId);
        Assert.Equal(HttpStatusCode.Created, (await host.Client.SendAsync(adminCrossQr)).StatusCode);
    }

    [Fact]
    public async Task OrganizerEventCreationUsesAuthenticatedOrganizerId()
    {
        await using var host = await CreateHostAsync();
        var data = await SeedResourcesAsync(host);
        var payload = $$"""{"organizerId":"{{data.OrganizerBId}}","title":"New event","category":"Community","startDate":"2026-11-01T09:00:00Z","endDate":"2026-11-01T17:00:00Z"}""";

        using var request = CreateRequest(HttpMethod.Post, "/api/events", AuthorizationRoles.Organizer, data.OrganizerAId, payload);
        var response = await host.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EventResponseBody>();
        Assert.NotNull(body);
        Assert.Equal(data.OrganizerAId, body!.OrganizerId);
    }

    [Fact]
    public async Task RoleRequirementsAreScopedThroughTheirParentEvent()
    {
        await using var host = await CreateHostAsync();
        var data = await SeedResourcesAsync(host);

        using var foreign = CreateRequest(HttpMethod.Get, $"/api/role-requirements?eventId={data.EventAId}", AuthorizationRoles.Organizer, data.OrganizerBId);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.SendAsync(foreign)).StatusCode);

        using var own = CreateRequest(HttpMethod.Get, $"/api/role-requirements?eventId={data.EventAId}", AuthorizationRoles.Organizer, data.OrganizerAId);
        Assert.Equal(HttpStatusCode.OK, (await host.Client.SendAsync(own)).StatusCode);
    }

    [Fact]
    public void ControllerAndActionRoleMetadataMatchesTheRolePlan()
    {
        AssertControllerRole<AgentController>(AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<AssignmentController>(AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<EventController>(AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<EventsController>(AuthorizationRoles.All);
        AssertActionRole<EventsController>(nameof(EventsController.GetAll), AuthorizationRoles.All);
        AssertActionRole<EventsController>(nameof(EventsController.GetById), AuthorizationRoles.All);
        AssertActionRole<EventsController>(nameof(EventsController.Create), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<EventsController>(nameof(EventsController.Update), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<EventsController>(nameof(EventsController.Delete), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<EventsController>(nameof(EventsController.UpdateStatus), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<EventsController>(nameof(EventsController.AddRole), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<EventsController>(nameof(EventsController.RemoveRole), AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<VenuesController>(AuthorizationRoles.All);
        AssertActionRole<VenuesController>(nameof(VenuesController.GetAll), AuthorizationRoles.All);
        AssertActionRole<VenuesController>(nameof(VenuesController.GetById), AuthorizationRoles.All);
        AssertActionRole<VenuesController>(nameof(VenuesController.Create), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<VenuesController>(nameof(VenuesController.Update), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<VenuesController>(nameof(VenuesController.Delete), AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<RoleRequirementController>(AuthorizationRoles.All);
        AssertControllerRole<ShiftController>(AuthorizationRoles.All);
        AssertActionRole<ShiftController>(nameof(ShiftController.Create), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<ShiftController>(nameof(ShiftController.Update), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<ShiftController>(nameof(ShiftController.Delete), AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<AttendanceController>(null);
        AssertActionRole<AttendanceController>(nameof(AttendanceController.GetByShift), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<AttendanceController>(nameof(AttendanceController.GetById), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<AttendanceController>(nameof(AttendanceController.CheckIn), AuthorizationRoles.Volunteer);
        AssertActionRole<AttendanceController>(nameof(AttendanceController.CheckOut), AuthorizationRoles.Volunteer);
        AssertControllerRole<QrTokenController>(AuthorizationRoles.AdminOrOrganizer);
        AssertControllerRole<ShiftSwapController>(AuthorizationRoles.All);
        AssertActionRole<ShiftSwapController>(nameof(ShiftSwapController.Approve), AuthorizationRoles.AdminOrOrganizer);
        AssertActionRole<ShiftSwapController>(nameof(ShiftSwapController.RejectByOrganizer), AuthorizationRoles.AdminOrOrganizer);
    }

    private static void AssertControllerRole<TController>(string? role)
    {
        var authorize = typeof(TController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>().SingleOrDefault();
        Assert.NotNull(authorize);
        Assert.Equal(role, authorize!.Roles);
    }

    private static void AssertActionRole<TController>(string methodName, string? role)
    {
        var method = typeof(TController).GetMethod(methodName);
        Assert.NotNull(method);
        var authorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>().SingleOrDefault();
        if (authorize is null)
        {
            AssertControllerRole<TController>(role);
            return;
        }
        if (role is null)
        {
            Assert.Null(authorize.Roles);
            return;
        }
        Assert.Equal(role, authorize.Roles);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string role, string? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-Role", role);
        if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        return request;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string role, Guid userId, string? body = null)
    {
        var request = CreateRequest(method, path, role, body);
        request.Headers.Add("X-Test-UserId", userId.ToString());
        return request;
    }

    private static HttpRequestMessage AttendanceRequest(string path, OwnershipData data, Guid suppliedProfileId, string token, Guid userId) =>
        CreateRequest(HttpMethod.Post, path, AuthorizationRoles.Volunteer, userId,
            $$"""{"shiftId":"{{data.ShiftAId}}","volunteerId":"{{suppliedProfileId}}","token":"{{token}}"}""");

    private static string EventUpdateJson(string title) =>
        $$"""{"title":"{{title}}","category":"Community","description":"Updated","startDate":"2026-11-01T09:00:00Z","endDate":"2026-11-01T17:00:00Z"}""";

    private static string ShiftUpdateJson(Guid eventId, Guid roleRequirementId) =>
        $$"""{"title":"Updated shift","eventId":"{{eventId}}","roleRequirementId":"{{roleRequirementId}}","startTime":"2026-11-01T09:00:00Z","endTime":"2026-11-01T10:00:00Z","capacity":3}""";

    private static async Task<OwnershipData> SeedResourcesAsync(TestHostHandle host)
    {
        using var scope = host.App.Services.CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventDb = scope.ServiceProvider.GetRequiredService<EventCrewDbContext>();
        var organizerA = new User { Id = Guid.NewGuid(), FullName = "Organizer A", Email = "orga@example.test", Role = AuthorizationRoles.Organizer, IsActive = true };
        var organizerB = new User { Id = Guid.NewGuid(), FullName = "Organizer B", Email = "orgb@example.test", Role = AuthorizationRoles.Organizer, IsActive = true };
        var admin = new User { Id = Guid.NewGuid(), FullName = "Administrator", Email = "admin@example.test", Role = AuthorizationRoles.Admin, IsActive = true };
        var volunteerA = new User { Id = Guid.NewGuid(), FullName = "Volunteer A", Email = "vola@example.test", Role = AuthorizationRoles.Volunteer, IsActive = true };
        var volunteerB = new User { Id = Guid.NewGuid(), FullName = "Volunteer B", Email = "volb@example.test", Role = AuthorizationRoles.Volunteer, IsActive = true };
        var eventA = CreateEvent(organizerA.Id, "Event A");
        var eventB = CreateEvent(organizerB.Id, "Event B");
        appDb.Users.AddRange(organizerA, organizerB, admin, volunteerA, volunteerB);
        appDb.Events.AddRange(eventA, eventB);
        await appDb.SaveChangesAsync();

        var requirement = new RoleRequirement { Id = Guid.NewGuid(), EventId = eventA.Id, Event = eventA, RoleName = "Guide", RequiredHeadcount = 4 };
        var requirementB = new RoleRequirement { Id = Guid.NewGuid(), EventId = eventB.Id, Event = eventB, RoleName = "Host", RequiredHeadcount = 2 };
        var shift = new Shift
        {
            Id = Guid.NewGuid(), EventId = eventA.Id, RoleRequirementId = requirement.Id, Event = eventA,
            RoleRequirement = requirement, Title = "Event A shift", StartTime = DateTimeOffset.UtcNow.AddHours(1),
            EndTime = DateTimeOffset.UtcNow.AddHours(2), Capacity = 4, Status = "Scheduled",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        var shiftB = new Shift
        {
            Id = Guid.NewGuid(), EventId = eventB.Id, RoleRequirementId = requirementB.Id, Event = eventB,
            RoleRequirement = requirementB, Title = "Event B shift", StartTime = DateTimeOffset.UtcNow.AddHours(3),
            EndTime = DateTimeOffset.UtcNow.AddHours(4), Capacity = 2, Status = "Scheduled",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        var profileA = new VolunteerProfile { Id = Guid.NewGuid(), UserId = volunteerA.Id };
        var profileB = new VolunteerProfile { Id = Guid.NewGuid(), UserId = volunteerB.Id };
        var eventDbUsers = new[]
        {
            CopyUser(organizerA), CopyUser(organizerB), CopyUser(admin), CopyUser(volunteerA), CopyUser(volunteerB)
        };
        eventA.Organizer = null!;
        eventB.Organizer = null!;
        var assignmentA = CreateAssignment(shift, profileA);
        var assignmentB = CreateAssignment(shift, profileB);
        profileA.User = eventDbUsers.Single(user => user.Id == volunteerA.Id);
        profileB.User = eventDbUsers.Single(user => user.Id == volunteerB.Id);
        eventDb.Users.AddRange(eventDbUsers);
        eventDb.RoleRequirements.AddRange(requirement, requirementB);
        eventDb.Shifts.AddRange(shift, shiftB);
        eventDb.VolunteerProfiles.AddRange(profileA, profileB);
        eventDb.ShiftAssignments.AddRange(assignmentA, assignmentB);
        await eventDb.SaveChangesAsync();

        return new OwnershipData(organizerA.Id, organizerB.Id, admin.Id, volunteerA.Id, volunteerB.Id,
            eventA.Id, eventB.Id, requirement.Id, requirementB.Id, shift.Id, shiftB.Id, profileA.Id, profileB.Id);
    }

    private static Event CreateEvent(Guid organizerId, string title) => new()
    {
        Id = Guid.NewGuid(), OrganizerId = organizerId, Title = title, Category = "Community",
        StartDate = DateTimeOffset.UtcNow.AddDays(1), EndDate = DateTimeOffset.UtcNow.AddDays(1).AddHours(8)
    };

    private static User CopyUser(User user) => new()
    {
        Id = user.Id, FullName = user.FullName, Email = user.Email, Role = user.Role,
        IsActive = user.IsActive, PasswordHash = user.PasswordHash
    };

    private static ShiftAssignment CreateAssignment(Shift shift, VolunteerProfile volunteer)
    {
        var now = DateTimeOffset.UtcNow;
        return new ShiftAssignment
        {
            Id = Guid.NewGuid(), ShiftId = shift.Id, Shift = shift, VolunteerId = volunteer.Id, Volunteer = volunteer,
            Status = "Confirmed", AssignedAt = now, CreatedAt = now, UpdatedAt = now
        };
    }

    private static async Task AddQrTokenAsync(TestHostHandle host, Guid shiftId, string rawToken)
    {
        using var scope = host.App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventCrewDbContext>();
        db.QrCodeTokens.Add(new QrCodeToken
        {
            Id = Guid.NewGuid(), ShiftId = shiftId,
            TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), IsActive = true, CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task<int> AttendanceCountAsync(TestHostHandle host)
    {
        using var scope = host.App.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<EventCrewDbContext>().AttendanceRecords.CountAsync();
    }

    private static async Task<TestHostHandle> CreateHostAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var databaseName = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<EventCrewDbContext>(options => options.UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddScoped<IEventService, EventService>();
        builder.Services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, RoleHeaderAuthenticationHandler>("Test", _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(ShiftController).Assembly);

        var app = builder.Build();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        return new TestHostHandle(app, app.GetTestClient());
    }

    private sealed class TestHostHandle : IAsyncDisposable
    {
        private readonly WebApplication _app;
        public WebApplication App => _app;
        public HttpClient Client { get; }

        public TestHostHandle(WebApplication app, HttpClient client)
        {
            _app = app;
            Client = client;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await _app.DisposeAsync();
        }
    }

    private sealed class RoleHeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            StringValues role = Request.Headers["X-Test-Role"];
            if (StringValues.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new List<Claim> { new(ClaimTypes.Role, role.ToString()) };
            if (Guid.TryParse(Request.Headers["X-Test-UserId"], out var userId)) claims.Add(new Claim("sub", userId.ToString()));
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed record OwnershipData(
        Guid OrganizerAId,
        Guid OrganizerBId,
        Guid AdminUserId,
        Guid VolunteerAUserId,
        Guid VolunteerBUserId,
        Guid EventAId,
        Guid EventBId,
        Guid RoleRequirementAId,
        Guid RoleRequirementBId,
        Guid ShiftAId,
        Guid ShiftBId,
        Guid VolunteerAProfileId,
        Guid VolunteerBProfileId);

    private sealed record EventResponseBody(Guid Id, Guid OrganizerId);
    private sealed record ShiftResponseBody(Guid Id);
}
