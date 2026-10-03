using System.Text.Json;
using System.Net.Http.Json;
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
/// 2. Persist the workflow run (status = AwaitingApproval) and tool-call audit trail.
/// 3. Manage human approval: approve / reject with audit info.
/// 4. Return structured DTOs to the controller.
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

    // ============================================================
    // PLAN — runs workflow, leaves run in AwaitingApproval
    // ============================================================
    public async Task<WorkflowRunStatusDto> PlanStaffingAsync(
        Guid eventId,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var eventEntity = await _db.Events
            .AsNoTracking()
            .Include(item => item.Venue)
            .Include(item => item.RoleRequirements)
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        if (eventEntity is null)
            throw new InvalidOperationException($"Event '{eventId}' was not found.");

        var request = new PlanRequestDto
        {
            EventId = eventEntity.Id.ToString(),
            Event = new PlanEventContextDto
            {
                Id = eventEntity.Id.ToString(),
                VenueId = eventEntity.VenueId?.ToString(),
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Category = eventEntity.Category,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Status = eventEntity.Status.ToString(),
                RoleRequirements = eventEntity.RoleRequirements.Select(role => new PlanRoleRequirementContextDto
                {
                    Id = role.Id.ToString(),
                    RoleName = role.RoleName,
                    RequiredHeadcount = role.RequiredHeadcount,
                    MinExperienceLevel = role.MinExperienceLevel.ToString()
                }).ToList()
            },
            Venue = eventEntity.Venue is null ? null : new PlanVenueContextDto
            {
                Id = eventEntity.Venue.Id.ToString(),
                Name = eventEntity.Venue.Name,
                Address = eventEntity.Venue.Address,
                City = eventEntity.Venue.City,
                Latitude = eventEntity.Venue.Latitude,
                Longitude = eventEntity.Venue.Longitude,
                Capacity = eventEntity.Venue.Capacity
            }
        };

        _logger.LogInformation("Calling AI service for event {EventId}", eventId);

        PlanResultDto plan;
        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/agent/plan",
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException("AI service could not create a plan for this event.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AI service returned status {StatusCode} for event {EventId}",
                    (int)response.StatusCode,
                    eventId);
                throw new AiServiceUnavailableException("AI planning service returned an error.");
            }

            var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                plan = JsonSerializer.Deserialize<PlanResultDto>(resultJson, JsonOptions)
                    ?? throw new JsonException("The AI service response was empty.");
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "AI service returned an invalid plan for event {EventId}", eventId);
                throw new AiServiceUnavailableException("AI planning service returned an invalid response.", ex);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "AI service unreachable for event {EventId}", eventId);
            throw new AiServiceUnavailableException("AI planning service is unreachable.", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "AI service timed out for event {EventId}", eventId);
            throw new AiServiceUnavailableException("AI planning service timed out.", ex);
        }

        if (!Guid.TryParse(plan.EventId, out var plannedEventId) || plannedEventId != eventId ||
            !string.Equals(plan.Status, "planned", StringComparison.OrdinalIgnoreCase) ||
            plan.Steps.Count == 0 || plan.ToolCalls.Count == 0)
        {
            throw new AiServiceUnavailableException("AI planning service returned an incomplete or mismatched plan.");
        }

        _logger.LogInformation(
            "AI service returned plan for event {EventId}: {Steps} steps, {ToolCalls} tool calls",
            eventId, plan.Steps.Count, plan.ToolCalls.Count);

        // ------------------------------------------------------------
        // 2. Persist the workflow run + tool logs, status = AwaitingApproval
        // ------------------------------------------------------------
        var run = await PersistWorkflowRunAsync(eventId, initiatedByUserId, plan, cancellationToken);

        return new WorkflowRunStatusDto
        {
            RunId = run.Id,
            EventId = run.EventId,
            Status = run.Status,
            Objective = run.PromptObjective,
            CreatedAt = run.CreatedAt
        };
    }

    // ============================================================
    // GET — full run details
    // ============================================================
    public async Task<WorkflowRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .AsNoTracking()
            .Include(r => r.ToolLogs)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        return run is null ? null : MapRunToDto(run);
    }

    // ============================================================
    // APPROVE — AwaitingApproval → Approved
    // ============================================================
    public async Task<WorkflowRunDetailDto?> ApproveAsync(Guid runId, Guid reviewedByUserId, CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .Include(r => r.ToolLogs)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run is null)
            return null;

        if (run.Status != "AwaitingApproval")
            throw new InvalidOperationException(
                $"Cannot approve a run in '{run.Status}' status. Only 'AwaitingApproval' runs can be approved.");

        run.Status = "Approved";
        run.ReviewedByUserId = reviewedByUserId;
        run.ReviewNotes = "Approved.";
        run.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workflow run {RunId} approved by user {UserId}", runId, reviewedByUserId);

        return MapRunToDto(run);
    }

    // ============================================================
    // REJECT — AwaitingApproval → Rejected (with reason)
    // ============================================================
    public async Task<WorkflowRunDetailDto?> RejectAsync(Guid runId, Guid reviewedByUserId, string reason, CancellationToken cancellationToken = default)
    {
        var run = await _db.AgentWorkflowRuns
            .Include(r => r.ToolLogs)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

        if (run is null)
            return null;

        if (run.Status != "AwaitingApproval")
            throw new InvalidOperationException(
                $"Cannot reject a run in '{run.Status}' status. Only 'AwaitingApproval' runs can be rejected.");

        run.Status = "Rejected";
        run.ReviewedByUserId = reviewedByUserId;
        run.ReviewNotes = reason;
        run.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Workflow run {RunId} rejected by user {UserId}", runId, reviewedByUserId);

        return MapRunToDto(run);
    }

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================
    private async Task<AgentWorkflowRun> PersistWorkflowRunAsync(
        Guid eventId,
        Guid initiatedByUserId,
        PlanResultDto plan,
        CancellationToken cancellationToken)
    {
        var planJson = JsonSerializer.Serialize(plan);

        var run = new AgentWorkflowRun
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InitiatedByUserId = initiatedByUserId,
            Status = "AwaitingApproval",     // ← paused for human approval
            PromptObjective = plan.Objective,
            PlanSummary = planJson,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

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
                CalledAt = DateTimeOffset.TryParse(call.CalledAt, out var calledAt)
                    ? calledAt
                    : DateTimeOffset.UtcNow
            });
        }

        _db.AgentWorkflowRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Persisted workflow run {RunId} with {Count} tool log entries for event {EventId}",
            run.Id, run.ToolLogs.Count, eventId);

        return run;
    }

    private static WorkflowRunDetailDto MapRunToDto(AgentWorkflowRun run) => new()
    {
        RunId = run.Id,
        EventId = run.EventId,
        InitiatedByUserId = run.InitiatedByUserId,
        Status = run.Status,
        Objective = run.PromptObjective,
        PlanSummary = run.PlanSummary,
        ReviewedByUserId = run.ReviewedByUserId,
        ReviewNotes = run.ReviewNotes,
        CreatedAt = run.CreatedAt,
        UpdatedAt = run.UpdatedAt,
        ToolLogs = run.ToolLogs.OrderBy(log => log.CalledAt).Select(log => new WorkflowToolLogDto
        {
            LogId = log.Id,
            AgentName = log.AgentName,
            ToolName = log.ToolName,
            InputParameters = log.InputParameters,
            OutputSummary = log.OutputSummary,
            ExecutionDurationMs = log.ExecutionDurationMs,
            CalledAt = log.CalledAt
        }).ToList()
    };
}

public sealed class AiServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);