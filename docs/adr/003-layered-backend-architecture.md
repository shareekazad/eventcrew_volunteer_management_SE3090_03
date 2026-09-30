# ADR-003: Layered Backend Architecture

- **Date:** 2026-09-24
- **Status:** Accepted
- **Deciders:** IT24104324 (backend scaffolding owner)

## Context

The ASP.NET Core backend must support:
- **4 students working in parallel** on different business components (venues/events, volunteers, scheduling, attendance)
- Clear ownership boundaries to avoid merge conflicts
- Independent testability of business logic
- A clean contract for the frontends (React + Flutter) — DTOs, not raw entities
- Future additions: JWT authentication, background jobs, third-party integrations

The spec explicitly rewards "suitable architecture" (Section 16.1) and requires evidence of framework-level thinking in the ADR.

We need a project structure that:
- Keeps domain models stable across the codebase
- Isolates EF Core / database concerns from business logic
- Makes controllers thin and services testable
- Scales to 4 developers without stepping on each other's files

## Options Considered

### 1. Single project (all code in `EventCrew.Api`)
- ✅ Fastest to set up
- ✅ Fewer files to navigate
- ❌ Everything is coupled — DTOs, entities, controllers, DbContext mixed
- ❌ Merge conflicts likely when 4 devs touch controllers + entities simultaneously
- ❌ Business rules buried in controllers are hard to unit test
- ❌ Poor fit for "suitable architecture" in the rubric

### 2. Two projects (`Api` + `Data`)
- ✅ Cleaner than single project
- ❌ Still mixes domain entities with infrastructure concerns
- ❌ Harder to enforce layer boundaries as the codebase grows
- ⚠️ Middle ground that doesn't fully address testing or separation

### 3. Three projects: `Api` / `Domain` / `Infrastructure` (+ separate `Tests`)
- ✅ **Clean layered separation** aligned with industry standards
- ✅ **Domain** holds entities + enums only — no dependencies
- ✅ **Infrastructure** holds EF Core DbContext + migrations — depends only on Domain
- ✅ **Api** holds controllers, DTOs, services, Program.cs — depends on both
- ✅ **Tests** project separate from production code
- ✅ Each layer has a single, clear responsibility
- ✅ Forces good habits: entity classes never leak to clients (DTOs used instead)
- ⚠️ Slightly more ceremony for small features

## Decision

**Use a 3-project layered solution + a separate test project.**

### Structure
EventCrew.sln
├── src/
│ ├── EventCrew.Api/ ← Controllers, DTOs, Services, Program.cs, appsettings
│ ├── EventCrew.Domain/ ← Entities, Enums (no external dependencies)
│ └── EventCrew.Infrastructure/ ← AppDbContext, EF Core configuration
└── tests/
└── EventCrew.Api.Tests/ ← xUnit unit tests


### Dependency direction
Api ─────► Infrastructure ─────► Domain
└────────────────────────────► Domain

- `Domain` depends on **nothing** (only the .NET base class library)
- `Infrastructure` depends on `Domain`
- `Api` depends on both
- `Tests` depends on `Api` + `Infrastructure` (via project references)

**No circular dependencies.** Enforced by .csproj project references.

### Layer responsibilities

| Layer | Contains | Owns |
|-------|----------|------|
| **Domain** | `Event`, `Venue`, `RoleRequirement`, `User`, enums | Business entities (pure C#) |
| **Infrastructure** | `AppDbContext` | Database mapping, EF Core config |
| **Api** | Controllers, DTOs, Services, `Program.cs` | HTTP contracts, business logic, DI wiring |
| **Tests** | xUnit test classes | Test coverage for services |

### Where business logic lives

**In services, not controllers.** For example:

- `VenueService` — venue CRUD, mapping, validation
- `EventService` — event CRUD, nested roles, status transitions
- `AgentService` — calls Python AI service, persists workflow

Controllers receive HTTP requests, delegate to services, and translate results to HTTP responses. They contain **zero** business logic.

## Consequences

**Positive:**
- **Parallel development:** 4 students work in different folders without merge conflicts
- **Testable:** services testable in isolation with an in-memory or mocked DbContext
- **Clean contracts:** entities never leaked over HTTP; DTOs control the API surface
- **Enforced boundaries:** project references prevent accidental coupling
- **Scalable:** adding a new feature = new DTOs + service + controller; existing layers unchanged
- **Aligned with rubric:** demonstrates framework/architecture understanding (LO2, LO3)

**Negative:**
- Slightly more files to navigate (mitigated by clear folder structure and naming)
- New developers must understand the layering (mitigated by this ADR + README)

**Neutral:**
- If we later split `Api` into separate microservices, the layering already supports it

## References

- Microsoft docs: "Common web application architectures"
- Implementation: `src/EventCrew.Api/`, `src/EventCrew.Domain/`, `src/EventCrew.Infrastructure/`
- Tests: `tests/EventCrew.Api.Tests/`
