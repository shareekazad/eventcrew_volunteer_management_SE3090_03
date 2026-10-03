using System.Net;
using System.Net.Http.Json;
using EventCrew.Api.DTOs.Agent;
using EventCrew.Api.Services;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventCrew.Api.Tests;

public sealed class AgentServiceTests
{
    [Fact]
    public async Task PlanningSendsAuthorizedEventContextAndPersistsInitiatorAndToolAudit()
    {
        await using var db = CreateDb();
        var (eventEntity, venue, initiatorId) = await SeedEventAsync(db);
        HttpRequestMessage? request = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler((message, _) =>
        {
            request = message;
            return Task.FromResult(PlanResponse(eventEntity.Id));
        }));
        httpClient.BaseAddress = new Uri("http://ai-service");
        var service = new AgentService(httpClient, db, NullLogger<AgentService>.Instance);

        var result = await service.PlanStaffingAsync(eventEntity.Id, initiatorId);

        Assert.Equal("AwaitingApproval", result.Status);
        Assert.Null(request!.Headers.Authorization);
        var requestJson = await request.Content!.ReadAsStringAsync();
        Assert.Contains(venue.Name, requestJson);
        Assert.Contains("Guide", requestJson);
        Assert.DoesNotContain("password", requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", requestJson, StringComparison.OrdinalIgnoreCase);

        var run = await db.AgentWorkflowRuns.SingleAsync();
        Assert.Equal(initiatorId, run.InitiatedByUserId);
        Assert.Equal(3, await db.AgentToolLogs.CountAsync());

        var details = await service.GetRunAsync(run.Id);
        Assert.NotNull(details);
        Assert.Equal(3, details!.ToolLogs.Count);
        Assert.Contains("staffing_recommendations", details.PlanSummary);
    }

    [Fact]
    public async Task PlanningServiceMapsTimeoutToUnavailableException()
    {
        await using var db = CreateDb();
        var (eventEntity, _, initiatorId) = await SeedEventAsync(db);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            (_, _) => throw new TaskCanceledException("request timed out")));
        httpClient.BaseAddress = new Uri("http://ai-service");
        var service = new AgentService(httpClient, db, NullLogger<AgentService>.Instance);

        var exception = await Assert.ThrowsAsync<AiServiceUnavailableException>(
            () => service.PlanStaffingAsync(eventEntity.Id, initiatorId));

        Assert.Contains("timed out", exception.Message);
        Assert.Empty(db.AgentWorkflowRuns);
    }

    [Fact]
    public async Task PlanningServiceMapsUpstreamFailureToUnavailableException()
    {
        await using var db = CreateDb();
        var (eventEntity, _, initiatorId) = await SeedEventAsync(db);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
        httpClient.BaseAddress = new Uri("http://ai-service");
        var service = new AgentService(httpClient, db, NullLogger<AgentService>.Instance);

        await Assert.ThrowsAsync<AiServiceUnavailableException>(
            () => service.PlanStaffingAsync(eventEntity.Id, initiatorId));
        Assert.Empty(db.AgentWorkflowRuns);
    }

    [Fact]
    public async Task ReviewTransitionsOnlyAllowAwaitingApprovalRuns()
    {
        await using var db = CreateDb();
        var (eventEntity, _, initiatorId) = await SeedEventAsync(db);
        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            (_, _) => Task.FromResult(PlanResponse(eventEntity.Id))));
        httpClient.BaseAddress = new Uri("http://ai-service");
        var service = new AgentService(httpClient, db, NullLogger<AgentService>.Instance);
        var created = await service.PlanStaffingAsync(eventEntity.Id, initiatorId);

        var approved = await service.ApproveAsync(created.RunId, initiatorId);
        Assert.Equal("Approved", approved!.Status);
        Assert.Equal(initiatorId, approved.ReviewedByUserId);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectAsync(created.RunId, initiatorId, "Too late"));

        var secondRun = await service.PlanStaffingAsync(eventEntity.Id, initiatorId);
        var rejected = await service.RejectAsync(secondRun.RunId, initiatorId, "Role requirements need revision");
        Assert.Equal("Rejected", rejected!.Status);
        Assert.Equal("Role requirements need revision", rejected.ReviewNotes);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(secondRun.RunId, initiatorId));
    }

    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(Event Event, Venue Venue, Guid InitiatorId)> SeedEventAsync(AppDbContext db)
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = "Community Hall",
            Address = "1 Main Street",
            City = "Colombo",
            Capacity = 500
        };
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            OrganizerId = Guid.NewGuid(),
            VenueId = venue.Id,
            Venue = venue,
            Title = "Volunteer Expo",
            Category = "Community",
            StartDate = DateTimeOffset.UtcNow.AddDays(10),
            EndDate = DateTimeOffset.UtcNow.AddDays(10).AddHours(8)
        };
        eventEntity.RoleRequirements.Add(new RoleRequirement
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            Event = eventEntity,
            RoleName = "Guide",
            RequiredHeadcount = 4
        });
        db.Events.Add(eventEntity);
        await db.SaveChangesAsync();
        return (eventEntity, venue, Guid.NewGuid());
    }

    private static HttpResponseMessage PlanResponse(Guid eventId)
    {
        var response = new PlanResultDto
        {
            Objective = "Plan staffing for Volunteer Expo",
            EventId = eventId.ToString(),
            Steps = [new PlanStepDto { StepNumber = 1, Action = "Inspect", Agent = "PlanningAgent" }],
            Reasoning = "Derived from event role requirements and venue capacity.",
            ToolCalls =
            [
                new ToolCallDto { ToolName = "get_event", OutputSummary = "Event inspected", CalledAt = DateTimeOffset.UtcNow.ToString("O") },
                new ToolCallDto { ToolName = "get_venue", OutputSummary = "Venue inspected", CalledAt = DateTimeOffset.UtcNow.ToString("O") },
                new ToolCallDto { ToolName = "calculate_staffing_ratio", OutputSummary = "Capacity rule applied", CalledAt = DateTimeOffset.UtcNow.ToString("O") }
            ],
            StaffingRecommendations =
            [
                new RoleStaffingRecommendationDto
                {
                    RoleName = "Guide",
                    RequiredHeadcount = 4,
                    MinimumExperienceLevel = "Beginner"
                }
            ],
            NextAgent = "OrganizerReview",
            Status = "planned"
        };
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
