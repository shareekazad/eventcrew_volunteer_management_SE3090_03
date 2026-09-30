# EventCrew — Volunteer Management Platform

**SE3090 — Software Engineering Frameworks | Assignment 1 | Group 03**

A full-stack volunteer management system for events, combining a React web app for organizers, a Flutter mobile app for volunteers, an ASP.NET Core Web API backend, PostgreSQL, and a LangGraph-orchestrated Agentic AI subsystem that plans event staffing.

---

## 📋 Table of Contents

- [Business Problem](#business-problem)
- [User Roles](#user-roles)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Agentic AI Workflow](#agentic-ai-workflow)
- [Repository Structure](#repository-structure)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Testing](#testing)
- [Deployment](#deployment)
- [Individual Contributions](#individual-contributions)
- [Security Considerations](#security-considerations)
- [AI Usage Declaration](#ai-usage-declaration)

---

## Business Problem

Event organizers struggle to recruit, schedule, and coordinate volunteers efficiently. Manual processes lead to:
- Double-booked shifts and overlapping commitments
- Poor skill-to-role matching
- Inefficient staffing plans (over- or under-staffed events)
- No audit trail of who was assigned where and when
- Slow coordination between organizers and volunteers

**EventCrew** addresses this by combining a shared data model, mobile-first volunteer experience, and an AI-assisted planning workflow that recommends staffing based on venue capacity — with mandatory human approval before any high-impact action.

---

## User Roles

| Role | Primary Channel | Capabilities |
|------|-----------------|--------------|
| **Organizer** | React web app | Create events, configure venues and role requirements, review applications, approve AI-generated rosters, monitor live attendance |
| **Volunteer** | Flutter mobile app | Browse events, submit applications, view assigned shifts, scan QR codes to check in/out on-site |
| **Admin** | (backend-ready) | Platform administration (planned extension) |

---

## Features

### Organizer (React)
- Event creation wizard (basic info → venue → role requirements → review)
- Venue management with capacity tracking
- AI-generated staffing plan review and approval (human-in-the-loop)
- Live attendance monitoring (planned — student 4)

### Volunteer (Flutter)
- Event discovery feed with search and filters
- Profile setup with skill selection
- Application submission and status tracking
- QR code check-in at event site (planned — student 4)

### Backend (ASP.NET Core Web API)
- Layered architecture (Api / Domain / Infrastructure / Tests)
- DTO-first API contracts (no entity leakage)
- Business rules enforced in the service layer (status transitions, date ordering, FK validation)
- JWT authentication (planned)
- Swagger UI for interactive API exploration

### Agentic AI (LangGraph)
- PlanningAgent produces structured multi-step plans
- Allow-listed tools (`get_event`, `get_venue`, `calculate_staffing_ratio`)
- Persisted workflow runs + audit trail of every tool call
- Conditional edges ensure safe-failure short-circuiting
- Structured JSON output — no freeform text

---

## Technology Stack

| Area | Technology | Why |
|------|-----------|-----|
| **Backend** | ASP.NET Core 8 (C#) | Mandatory per spec; strong typing, built-in DI, EF Core integration |
| **Data Access** | EF Core 8 + Npgsql | Mature ORM, async-first, first-class Postgres support |
| **Database** | PostgreSQL 16 | Native `jsonb` for agent state, strong consistency, free hosting |
| **Web App** | React 18 + Vite + Zustand + React Router | Lightweight, fast HMR, minimal state boilerplate |
| **Mobile App** | Flutter 3.x + Riverpod + mobile_scanner | Single codebase for iOS/Android, strong typing, native QR access |
| **Agentic AI** | Python 3.11 + FastAPI + LangGraph | LangGraph from lab; FastAPI for clean HTTP contracts |
| **Testing** | xUnit + FluentAssertions + pytest | Standard tooling; async support |
| **CI/CD** | GitHub Actions | Free, native to GitHub, required by spec |
| **Deployment** | (see ADR-005) | TBD — Railway / Render / Fly.io |

Full rationale in [`docs/adr/`](docs/adr/).

---

## Architecture

```
┌──────────────────┐         ┌──────────────────┐
│   React (Web)    │         │  Flutter (Mobile)│
└────────┬─────────┘         └────────┬─────────┘
         │                            │
         │        HTTPS / JSON        │
         └────────────┬───────────────┘
                      ▼
         ┌───────────────────────────┐
         │  ASP.NET Core Web API     │
         │  (REST endpoints, JWT,    │
         │   business rules, audit)  │
         └──────┬──────────────┬─────┘
                │              │
                │              │ (internal HTTP)
                ▼              ▼
      ┌──────────────┐  ┌───────────────────┐
      │ PostgreSQL   │  │  Python AI Service│
      │ (15 tables)  │  │  (FastAPI +       │
      │              │  │   LangGraph)      │
      └──────────────┘  └───────────────────┘
```

**Rules enforced:**
- React and Flutter communicate **only** with the ASP.NET Core API
- The Python AI service is **only** called by ASP.NET Core
- All persistence goes through ASP.NET Core → PostgreSQL

Detailed architecture: [`docs/architecture.md`](docs/architecture.md)

---

## Agentic AI Workflow

The minimum acceptance workflow:

1. **Organizer triggers planning** → `POST /api/Agent/plan/{eventId}` on ASP.NET Core
2. **ASP.NET Core calls Python** → `POST /agent/plan` on the FastAPI service
3. **LangGraph orchestrates nodes:**
   - `fetch_event_node` → tool: `get_event`
   - `fetch_venue_node` → tool: `get_venue`
   - `calculate_ratio_node` → tool: `calculate_staffing_ratio`
   - `build_plan_node` → assembles structured plan, delegates to MatchingAgent
4. **Graph short-circuits to END on any node failure** (conditional edges)
5. **Python returns `PlanResult`** → ASP.NET Core persists to `agent_workflow_runs` + `agent_tool_logs`
6. **Organizer reviews and approves** (human-in-the-loop — Section 9.1)

**Agentic evidence:** structured plan, distinct agent roles, allow-listed tools, persisted state, deterministic validation (DB-level constraints + service-layer business rules), audit trail, safe failure.

Full evaluation: [`docs/agentic-ai-evaluation.md`](docs/agentic-ai-evaluation.md)

---

## Repository Structure

```
eventcrew_volunteer_management_SE3090_03/
├── .github/workflows/ci.yml       GitHub Actions CI
├── docker-compose.yml             PostgreSQL container
├── database/init/01_schema.sql    15-table schema (auto-loads)
├── src/
│   ├── EventCrew.Api/             REST controllers, DTOs, services
│   ├── EventCrew.Domain/          Entities, enums
│   └── EventCrew.Infrastructure/  EF Core DbContext, migrations
├── tests/
│   └── EventCrew.Api.Tests/       xUnit tests (18)
├── ai-service/
│   ├── app/
│   │   ├── agents/                PlanningAgent
│   │   ├── graphs/                LangGraph nodes + graph
│   │   ├── tools/                 HTTP client + tools
│   │   └── main.py                FastAPI entry point
│   ├── tests/                     pytest tests (15)
│   └── requirements.txt
└── docs/
    ├── adr/                       Architecture Decision Records
    ├── architecture.md
    ├── ai-usage-log.md
    └── agentic-ai-evaluation.md
```

---

## Getting Started

### Prerequisites

- **.NET 8 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop** — [download](https://www.docker.com/products/docker-desktop/)
- **Python 3.11+**
- **Node.js 20+** (for React)
- **Flutter 3.x SDK** (for mobile)

### 1. Clone and configure

```bash
git clone https://github.com/shareekazad/eventcrew_volunteer_management_SE3090_03.git
cd eventcrew_volunteer_management_SE3090_03
```

### 2. Start PostgreSQL

```bash
docker compose up -d
```

Verify 15 tables:
```bash
docker exec -it eventcrew-postgres psql -U eventcrew -d eventcrew_db -c "\dt"
```

### 3. Run the ASP.NET Core API

```bash
dotnet run --project src/EventCrew.Api
```

Swagger UI: **http://localhost:5100/swagger**

### 4. Run the Python AI service

```bash
cd ai-service
python -m venv .venv
.\.venv\Scripts\Activate.ps1           # Windows
# source .venv/bin/activate            # macOS/Linux
pip install -r requirements.txt
uvicorn app.main:app --reload --port 8000
```

FastAPI docs: **http://localhost:8000/docs**

### 5. Environment variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `ConnectionStrings__DefaultConnection` | Postgres connection | see `appsettings.Development.json` |
| `AiService__BaseUrl` | Python AI service URL | `http://localhost:8000` |
| `BACKEND_BASE_URL` | ASP.NET Core URL used by AI service | `http://localhost:5100` |

---

## API Documentation

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/Venues` | GET, POST | List, create venues |
| `/api/Venues/{id}` | GET, PUT, DELETE | Single venue operations |
| `/api/Events` | GET, POST | List, create events |
| `/api/Events/{id}` | GET, PUT, DELETE | Single event operations |
| `/api/Events/{id}/status` | PATCH | Status transition (validated) |
| `/api/Events/{eventId}/roles` | POST | Add role requirement |
| `/api/Events/{eventId}/roles/{roleId}` | DELETE | Remove role requirement |
| `/api/Agent/plan/{eventId}` | POST | Trigger PlanningAgent workflow |

Full Swagger: `http://localhost:5100/swagger`

---

## Testing

### Backend (xUnit)

```bash
dotnet test tests/EventCrew.Api.Tests
```

Expected: **18 passed**

### AI service (pytest)

```bash
cd ai-service
pytest
```

Expected: **15 passed**

### CI

Every push and PR to `main` triggers [`.github/workflows/ci.yml`](.github/workflows/ci.yml) which builds and tests both projects.

---

## Deployment

- **Backend API:** *pending* — health URL + Swagger URL
- **React app:** *pending* — live URL
- **PostgreSQL:** *pending* — hosted instance
- **AI service:** *pending* — deployed or documented local run
- **Flutter APK:** *pending* — download link

Update this section as deployment lands.

---

## Individual Contributions

> Each student's detailed contribution is documented in the consolidated report (Section 3). Summary below.

| # | Student | Component | AI Agent |
|---|---------|-----------|----------|
| 1 | Shareeka Azad | Events, Venues, Role Requirements | **Planning & Coordinator Agent** (LangGraph) |
| 2 | *TBD* | Volunteer Profiles, Applications, Skills | **Volunteer Matching Agent** |
| 3 | *TBD* | Shifts, Shift Assignments, Shift Swaps | **Scheduling Agent** |
| 4 | *TBD* | Attendance, QR Tokens | **Validation & Safety Agent** |

---

## Security Considerations

- **No hardcoded secrets** — env vars only; `.gitignore` excludes `.env`
- **JWT authentication** (planned) with role-based authorization
- **DTO-based API contracts** — entities never leaked to clients
- **Input validation** at DTO layer (`[Required]`, `[Range]`, `[MaxLength]`)
- **Business rules enforced** in service layer, not controllers
- **SQL injection prevented** by EF Core parameterized queries
- **CORS** configured (planned)
- **Rate limiting** on AI endpoints (planned)
- **Prompt injection resistance** — AI service tool allow-list; no dynamic tool discovery
- **Safe failure** — AI graph short-circuits on error; no partial-state commits

Full list: [`docs/security.md`](docs/security.md) (to be added)

---

## AI Usage Declaration

AI tools (ChatGPT, Claude, GitHub Copilot) were used during **development** as permitted under Level 4 of the SE3090 AI Assessment Scale. All AI-generated code was reviewed, tested, and modified by the submitting student. No AI tools were used during the final demonstration or viva.

**Individual AI usage logs:** [`docs/ai-usage-log.md`](docs/ai-usage-log.md)

**Group AI usage declaration:** see consolidated report.

---

## License

Academic project — SLIIT SE3090 | Year 3 and Semester 1, 2026. Not licensed for redistribution.
