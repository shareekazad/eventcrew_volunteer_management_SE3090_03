# AI Usage Log — Shareeka Azad (IT24104324)

**Module:** SE3090 — Software Engineering Frameworks | Year 3 and Semester 1, 2026
**AI Use Level:** Level 4 (Full AI) during development;
**Tools used:** Claude (Anthropic), occasionally GitHub Copilot

This log records every significant use of AI assistance during the project. Each entry shows:
- **Date** — when the interaction happened
- **Tool** — which AI tool was used
- **Task** — what I was trying to do
- **What the tool produced** — the AI's output, summarized
- **What I changed/rejected** — my own intervention
- **How I verified** — the evidence that the output is correct

**Note on honesty:** This log reflects my actual development process. Entries correspond to commits visible in the Git history.

---

## Entry 1 — Environment setup & initial repo scaffolding
- **Date:** 2026-09-28
- **Tool:** Claude (Sonnet 4.5)
- **Task:** Setting up the development environment: clone repo, install Docker, PostgreSQL, ASP.NET Core solution structure
- **What the tool produced:**
  - Instructions to clone the GitHub repo
  - Docker Compose template for PostgreSQL 16
  - Layered .NET solution scaffolding commands (`dotnet new sln`, `classlib`, `webapi`, `xunit`)
  - Fixed the DDL script's `uuid-ossp` → `pgcrypto` extension mismatch
- **What I changed/rejected:**
  - **Rejected** the placeholder clone URL — replaced with the real repo URL from my team
  - **Chose** Docker over a native PostgreSQL install after comparing trade-offs
  - **Changed** the extension name after understanding that `gen_random_uuid()` comes from `pgcrypto`, not `uuid-ossp`
- **How I verified:**
  - `git remote -v` showed the correct remote
  - `docker compose up -d` started the container
  - `\dt` listed all **15 tables** as expected

---

## Entry 2 — Entity classes and AppDbContext
- **Date:** 2026-09-28
- **Tool:** Claude
- **Task:** Write C# entity classes for `Venue`, `Event`, `RoleRequirement` + EF Core `AppDbContext`
- **What the tool produced:**
  - Three entity classes with `[Table]`, `[Column]`, `[Key]` attributes mapping to snake_case DB columns
  - Enum classes for `EventStatus` and `ExperienceLevel`
  - `AppDbContext` with `DbSet` properties and `HasConversion<string>()` for enums
- **What I changed/rejected:**
  - **Rejected** the initial idea of also adding `HasCheckConstraint` in `AppDbContext` (would conflict with the existing DDL's CHECK constraints)
  - **Chose** to let the DDL own all DB constraints and keep EF Core configuration minimal
- **How I verified:**
  - `dotnet build` succeeded with 0 warnings, 0 errors
  - Later integration test against a real Postgres container confirmed inserts work

---

## Entry 3 — Venue CRUD (DTOs, Service, Controller)
- **Date:** 2026-09-28
- **Tool:** Claude
- **Task:** Implement full CRUD for the Venue feature (DTOs → Service → Controller)
- **What the tool produced:**
  - Three DTOs (`VenueResponseDto`, `CreateVenueDto`, `UpdateVenueDto`) with validation attributes
  - `IVenueService` interface + `VenueService` implementation
  - `VenuesController` with 5 REST endpoints
- **What I changed/rejected:**
  - **Understood and kept** the `AsNoTracking()` on read paths (velocity) and its absence on write paths (change tracking)
  - **Learned** why `[ApiController]` auto-validates DTOs — validated by sending invalid payloads and observing 400 responses
- **How I verified:**
  - Swagger POST returned 201 Created
  - `psql` SELECT confirmed the venue row
  - GET, PUT, DELETE all returned expected status codes (200, 204, 404)

---

## Entry 4 — Event CRUD with business rules
- **Date:** 2026-09-29
- **Tool:** Claude
- **Task:** Implement Event CRUD, nested role requirements, and status transition rules
- **What the tool produced:**
  - 6 DTOs including `UpdateEventStatusDto` with `[RegularExpression]` for status validation
  - `EventService` with a `Dictionary<EventStatus, EventStatus[]>` for legal status transitions
  - `EventsController` with 8 endpoints including nested role operations
- **What I changed/rejected:**
  - **Chose** separate endpoints for role requirements (`POST /events/{id}/roles`) instead of managing them in `UpdateEventDto` — cleaner REST, easier to test
  - **Rejected** allowing status in `UpdateEventDto` — status changes have business rules, so they need a dedicated endpoint
  - **Understood** why "not found" returns null (404) but "illegal transition" throws (400) — different exception semantics
- **How I verified:**
  - Created event with 2 nested roles in one request (verified by `psql` showing 2 role rows)
  - PATCH status `Draft → Published` returned 200
  - PATCH status `Draft → Completed` returned **400** with the correct error message
  - Nested role POST/DELETE worked with correct status codes

---

## Entry 5 — Python AI service (FastAPI + tools + PlanningAgent)
- **Date:** 2026-09-29 to 2026-09-30
- **Tool:** Claude
- **Task:** Build the AI service: FastAPI skeleton, HTTP client, three tools, PlanningAgent
- **What the tool produced:**
  - FastAPI app with `/health` and `/agent/plan` endpoints
  - `BackendClient` — httpx-based client with timeout handling and typed errors
  - Tools: `get_event`, `get_venue`, `calculate_staffing_ratio`
  - `PlanningAgent` class with structured `PlanResult` output and full tool-call audit log
- **What I changed/rejected:**
  - **Fixed** the initial 10s timeout → 30s after seeing cold-start latency of 9.3s in dev
  - **Added** `BackendError` exception to distinguish timeout vs. connection failure vs. HTTP error
  - **Understood** why tools use Pydantic models instead of dicts — validation and structured output
- **How I verified:**
  - Ran `python -m tests.test_planning_agent` and got the expected 4-step plan
  - Verified the tool-call log had correct durations

---

## Entry 6 — ASP.NET Core ↔ Python integration
- **Date:** 2026-09-30
- **Tool:** Claude
- **Task:** Wire ASP.NET Core to call the Python `/agent/plan` endpoint and persist the workflow
- **What the tool produced:**
  - `PlanRequestDto` and `PlanResultDto` with `[JsonPropertyName]` for snake_case mapping
  - `IAgentService` + `AgentService` that calls Python via typed `HttpClient`
  - `AgentWorkflowRun` and `AgentToolLog` entities
  - `AgentController` exposing `POST /api/Agent/plan/{eventId}`
- **What I changed/rejected:**
  - **Debugged** a 422 error from FastAPI — root cause was PascalCase JSON (`eventId`) vs. snake_case Pydantic field (`event_id`); fixed with `[JsonPropertyName("event_id")]`
  - **Debugged** an EF Core FK violation (`agent_tool_logs_workflow_run_id_fkey`) — root cause was EF Core inserting children before parents; fixed by adding a navigation property + `[ForeignKey]` so EF Core knows the dependency order
- **How I verified:**
  - Swagger returned **200 OK** with the plan JSON
  - `psql` confirmed 1 row in `agent_workflow_runs` + 3 rows in `agent_tool_logs`
  - Logs in ASP.NET Core + Uvicorn showed the full chain executed

---

## Entry 7 — LangGraph orchestration
- **Date:** 2026-09-30
- **Tool:** Claude
- **Task:** Replace the direct PlanningAgent call with a LangGraph state graph
- **What the tool produced:**
  - `PlanningState` (Pydantic model) — shared state across nodes
  - 4 node functions (fetch_event, fetch_venue, calculate_ratio, build_plan)
  - `StateGraph` wiring with conditional edges for safe-failure short-circuiting
  - `run_planning_graph(event_id)` — high-level entry point
- **What I changed/rejected:**
  - **Chose** conditional edges over sequential edges so a failed node stops the graph (safe failure)
  - **Kept** the agent class intact — LangGraph became the orchestrator, the agent stayed the worker
  - **Understood** why nodes return `state.tool_calls + [new_entry]` instead of `.append()` — LangGraph merges partial updates, not mutable state
- **How I verified:**
  - Same Swagger request returned the same `PlanResult` JSON
  - A new `agent_workflow_runs` row + 3 `agent_tool_logs` rows persisted
  - Uvicorn logs showed the node sequence

---

## Entry 8 — Test suites (xUnit + pytest)
- **Date:** 2026-09-30
- **Tool:** Claude
- **Task:** Write automated tests for backend services and Python AI subsystem
- **What the tool produced:**
  - 8 xUnit tests for `VenueService` + 10 for `EventService`
  - 10 pytest tests for `calculate_staffing_ratio` + 5 for the LangGraph workflow (mocked HTTP)
  - A fix in `EventService.CreateAsync` (remove double-add of nested roles to DbSet + navigation)
  - A fix in `planning_graph.py` (conditional edges to short-circuit on failure)
- **What I changed/rejected:**
  - **Kept** `AsNoTracking` in read tests (isolates test behavior)
  - **Chose** in-memory DB for tests (fast, no external dependency)
  - **Accepted** the two bug fixes the tests revealed — they were real issues
- **How I verified:**
  - `dotnet test` → **18 passed**
  - `pytest` → **15 passed**
  - The bug fixes were confirmed by re-running the failing tests

---

## Entry 9 — GitHub Actions CI
- **Date:** 2026-09-30
- **Tool:** Claude
- **Task:** Set up CI to run tests on every push and PR to main
- **What the tool produced:**
  - `.github/workflows/ci.yml` — two parallel jobs (backend + AI service)
- **What I changed/rejected:**
  - **Debugged** two CI failures:
    1. `pytest: command not found` — root cause: pytest wasn't in the committed `requirements.txt`
    2. Token missing `workflow` scope — fixed by creating a new Personal Access Token with the correct scope
  - **Understood** why GitHub requires explicit `workflow` scope for pushing workflow files
- **How I verified:**
  - GitHub Actions run #2 shows both jobs **green** with all tests passing

---
## Summary

| Category | Count |
|----------|-------|
| Entries logged | 10 |
| Bugs found by tests + fixed | 3 |
| Bugs found in integration | 2 |
| Times AI output was rejected or rewritten | 8 |
| Times AI output was verified with a test | 33 tests (18 C# + 15 Python) |

**Total AI usage:** Development-only. No external AI tools used during any evaluation, viva, or demonstration.

**Declaration:** I confirm the above is an accurate record of my AI tool usage. Every line of code committed under my name has been reviewed, tested, and understood by me.
