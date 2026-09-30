using System.Text;
using System.Text.Json;
using EventCrew.Api.DTOs.Agent;
using EventCrew.Domain.Entities;
using EventCrew.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventCrew.Api.Services;

/// <summary>
/// Service that bridges ASP.NET Core to the Python AI service.
///
/// Responsibilities:
/// 1. Call the Python /agent/plan endpoint over HTTP.
/// 2. Persist the workflow run and tool-call audit trail to PostgreSQL.
/// 3. Return the structured plan.
/// </summary>
public class AgentService : IAgentService
{
    private readonly HttpClient _http;
    private readonly AppDbContext _db;
    private readonly ILogger<AgentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AgentService(HttpClient http, AppDbContext db, ILogger<AgentService> logger)
    {
        _http = http;
        _db = db;
        _logger = logger;
    }

    public async Task<PlanResultDto> PlanStaffingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        // ----------------------------------------------------------------
        // 1. Call the Python AI service
        // ----------------------------------------------------------------
        var requestBody = JsonSerializer.Serialize(
            new PlanRequestDto { EventId = eventId.ToString() });

        using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        _logger.LogInformation("Calling AI service for event {EventId}", eventId);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/agent/plan", content, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI service unreachable for event {EventId}", eventId);
            throw new InvalidOperationException("AI service is unreachable.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "AI service timed out for event {EventId}", eventId);
            throw new InvalidOperationException("AI service timed out.", ex);
        }

        // Propagate business errors from Python (400 Bad Request)
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("AI service rejected plan for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service rejected the request: {detail}");
        }

        // Propagate service unavailable errors
        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("AI service unavailable for event {EventId}: {Detail}", eventId, detail);
            throw new InvalidOperationException($"AI service temporarily unavailable: {detail}");
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var plan = JsonSerializer.Deserialize<PlanResultDto>(json, JsonOptions)
                   ?? throw new InvalidOperationException("AI service returned an invalid plan.");

        _logger.LogInformation(
            "AI service returned plan for event {EventId}: {Steps} steps, {ToolCalls} tool calls",
            eventId, plan.Steps.Count, plan.ToolCalls.Count);

        // ----------------------------------------------------------------
        // 2. Persist the workflow run + tool logs
        // ----------------------------------------------------------------
        await PersistWorkflowRunAsync(eventId, plan, cancellationToken);

        return plan;
    }

    // ============================================================
    // Private helpers
    // ============================================================
    private async Task PersistWorkflowRunAsync(
        Guid eventId,
        PlanResultDto plan,
        CancellationToken cancellationToken)
    {
        // Find an organizer of this event to attribute the run to.
        // (Later this will come from the JWT-authenticated user.)
        var organizerId = await _db.Events
            .Where(e => e.Id == eventId)
            .Select(e => (Guid?)e.OrganizerId)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizerId is null)
        {
            _logger.LogWarning("Cannot persist workflow run: event {EventId} not found", eventId);
            return;
        }

        var planJson = JsonSerializer.Serialize(plan);

        var run = new AgentWorkflowRun
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InitiatedByUserId = organizerId.Value,
            Status = "Running",
            PromptObjective = plan.Objective,
            PlanSummary = planJson,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        // Add tool logs via the navigation property so EF Core orders
        // the INSERT correctly (parent first, then children).
        foreach (var call in plan.ToolCalls)
        {
            run.ToolLogs.Add(new AgentToolLog
            {
                Id = Guid.NewGuid(),
                AgentName = "PlanningAgent",
                ToolName = call.ToolName,
                InputParameters = JsonSerializer.Serialize(call.InputParams),
                OutputSummary = JsonSerializer.Serialize(new { summary = call.OutputSummary }),
                ExecutionDurationMs = call.DurationMs,
                CalledAt = DateTimeOffset.UtcNow
            });
        }

        _db.AgentWorkflowRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Persisted workflow run {RunId} with {Count} tool log entries for event {EventId}",
            run.Id, run.ToolLogs.Count, eventId);
    }
}