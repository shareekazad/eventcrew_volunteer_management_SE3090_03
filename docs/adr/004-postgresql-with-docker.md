# ADR-004: PostgreSQL via Docker for Local Development

- **Date:** 2026-09-23
- **Status:** Accepted
- **Deciders:** IT24103013

## Context

EventCrew uses PostgreSQL as its primary database. Four students develop on **different machines** running Windows, with varying prior setups (some have native PostgreSQL 17 installed, some have MySQL, some have nothing). Every student must be able to:
- Spin up a database in **under 5 minutes**
- Load the shared 15-table schema **identically** on every machine
- Iterate quickly without Docker/Kubernetes knowledge
- Reproduce bugs consistently across machines

Without a shared approach, three problems emerge:
1. **Version drift** — different Postgres versions cause subtle behavioral differences
2. **Setup friction** — each student reads different tutorials, misses steps
3. **Port conflicts** — a pre-existing native Postgres on port 5432 shadows our dev database

The spec requires (Section 12) that the DB be set up with **migrations, restricted credentials and initialization instructions**, and that the whole thing be **reproducible** (Section 14).

## Options Considered

### 1. Native PostgreSQL install per student
- ✅ Fastest raw performance
- ✅ No Docker dependency
- ❌ **Version drift** across machines (14/15/16/17)
- ❌ **Setup varies** — different install paths, service names, default ports
- ❌ **Port conflicts** with any pre-existing install (as we hit during development)
- ❌ Hard to reset to a clean state — manual drop/create
- ❌ Doesn't match a deployment environment cleanly

### 2. Cloud-hosted PostgreSQL (free tier from Neon / Railway / Supabase)
- ✅ No local install
- ✅ Same DB shared by all team members
- ❌ **Network dependency** — no offline development
- ❌ **Shared state** — one team member's test data pollutes everyone's
- ❌ **Loses the "reset and re-run schema" workflow**
- ❌ Adds a paid-tier ceiling
- ❌ Real risk: hitting free-tier limits during the 9-week project

### 3. PostgreSQL in Docker via `docker-compose.yml`
- ✅ **Identical environment** across all machines (postgres:16-alpine image)
- ✅ **One command to start** (`docker compose up -d`)
- ✅ **Automatic schema initialization** — `database/init/*.sql` runs on first boot
- ✅ **Isolated port binding** — easy to shift if 5432 conflicts (e.g., `5433:5432`)
- ✅ **Reset in one command** — `docker compose down -v && docker compose up -d`
- ✅ **Matches production** — deployment platform will also run Postgres in a container
- ✅ **Zero impact** on students' other coursework databases
- ⚠️ Requires Docker Desktop
- ⚠️ Slight performance overhead vs native (negligible for dev)

## Decision

**Use PostgreSQL 16 in Docker Compose for local development.**

### Configuration

- **Image:** `postgres:16-alpine` (smallest supported version)
- **Container name:** `eventcrew-postgres`
- **Port:** `5432:5432` (documented fallback to `5433:5432` if native Postgres conflicts)
- **Database:** `eventcrew_db`
- **User:** `eventcrew` / password `eventcrew_dev_password` (dev only — production uses env-managed secrets)
- **Volume:** `eventcrew_pgdata` — persists data between container restarts
- **Init scripts:** `database/init/*.sql` — auto-executed on first container creation
- **Reset workflow:** `docker compose down -v && docker compose up -d`

### Known environment issue

Some team members have native PostgreSQL 17 already running on port 5432 (from earlier coursework). The fix is documented in `README.md`:
Stop-Service postgresql-x64-17
Set-Service postgresql-x64-17 -StartupType Manual

This stops the native service; Docker's container then owns port 5432 cleanly.

## Consequences

**Positive:**
- **One-command setup:** `docker compose up -d` on any machine
- **Identical schema** — the DDL auto-loads; no manual `psql -f` steps
- **No drift** — everyone runs PostgreSQL 16
- **Easy reset** for demos, tests, or recovering from mistakes
- **Matches deployment** — cloud Postgres is also a container service
- **Doesn't interfere** with students' other work

**Negative:**
- Docker Desktop is a hard requirement (~500 MB install)
- Docker must be running for tests during development (not for CI — GitHub Actions runs its own Postgres)
- The `-v` flag on `docker compose down` is destructive; documented as "reset only"

**Neutral:**
- Containerized Postgres performance is fine for dev; production will use managed Postgres anyway

## Migration path

If we ever need to switch to a managed Postgres (Railway, Fly.io, Neon), the change is confined to `appsettings.Production.json` — the EF Core code, entities, and migrations are already database-agnostic.

## References

- Docker Compose file: `docker-compose.yml`
- Schema: `database/init/01_schema.sql`
- Setup instructions: `README.md` → "Getting Started"
- Known issue resolution: `README.md` → "Port 5432 conflict"

