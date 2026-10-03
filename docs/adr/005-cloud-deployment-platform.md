# ADR-005: Cloud Deployment Platform

- **Date:** 2026-09-30
- **Status:** Proposed (pending final deployment)
- **Deciders:** IT24102868, IT24102554

## Context

SE3090 requires (Section 14) that by the submission deadline:
- The **ASP.NET Core API** be deployed with a working health URL and Swagger URL
- The **PostgreSQL** instance be deployed securely with migrations and initialization instructions
- The **React app** be deployed and use the deployed API
- The **Flutter APK** be provided (not cloud-hosted, but installable)
- The **Agentic AI service** be deployed or documented as runnable locally with clear setup

Section 14 also states:
> "You must be able to complete this assignment using institution-provided or no-cost services. Paid subscriptions are not required."

We therefore need a platform that:
- Is **free** (or within free student tier)
- Supports **.NET 8** deployment
- Supports **PostgreSQL** with reasonable limits
- Supports **static hosting** for the React build
- Has **predictable sleep/limit behavior** for evaluation
- Allows **custom environment variables** for secrets

## Options Considered

### 1. Azure (for Students)
- ✅ $100 free credits for students
- ✅ First-class .NET support (App Service)
- ✅ Azure Database for PostgreSQL (free tier available)
- ✅ Static Web Apps (free for React)
- ❌ Complex pricing; easy to accidentally incur charges
- ❌ Cold starts on free App Service tier (~20s first request)
- ❌ Multi-service setup is verbose
- ❌ Student account verification can be slow

### 2. Railway
- ✅ **Simplest DX** — connect GitHub, deploy in 2 minutes
- ✅ First-class support for .NET (via Dockerfile or Nixpacks) and Node (React)
- ✅ Managed PostgreSQL included
- ✅ Environment variables via UI
- ✅ $5 free trial credit, then $5/month Hobby plan
- ⚠️ Free trial credit is limited — will expire during the 9-week project
- ⚠️ Requires paid plan for continued hosting after trial

### 3. Render
- ✅ **Free tier** for Web Services (with 15-min inactivity sleep)
- ✅ **Free PostgreSQL** for 90 days
- ✅ Supports **Docker** and native runtimes
- ✅ Static Sites free for React
- ✅ Simple env var management
- ⚠️ Free Postgres expires after 90 days (enough for our timeline)
- ⚠️ Free Web Services sleep — first request after inactivity takes ~30s

### 4. Fly.io
- ✅ Generous free allowances (3 shared VMs, 3 GB Postgres)
- ✅ Global edge deployment
- ✅ Docker-native
- ❌ Steeper learning curve (Fly CLI, `fly.toml`, regions)
- ❌ Postgres setup is more manual than Railway/Render
- ⚠️ Postgres free tier requires careful volume sizing

### 5. Self-hosted on a university VM
- ✅ No cost
- ❌ University VMs often reset / no public HTTPS by default
- ❌ Setup overhead
- ❌ Not guaranteed to be available during evaluation window

### 6. Combination approach (recommended)
- **API + AI service:** Render (free Web Service with Docker)
- **PostgreSQL:** Render (free Postgres, 90-day window — sufficient)
- **React:** Vercel or Netlify (free static hosting, instant deploy, custom domains)
- **Flutter APK:** GitHub Releases (free, version-controlled)

## Decision

**Adopt a combination approach — Render for API + AI + Postgres, and Vercel for React.**

### Rationale

| Component | Platform | Why |
|-----------|----------|-----|
| ASP.NET Core API | **Render** (Docker) | Free tier; Docker gives us control over the image; sleep is acceptable for evaluation |
| PostgreSQL | **Render** (free Postgres) | 90-day window covers the project timeline; managed backups; connection string injectable |
| AI Service (FastAPI) | **Render** (Docker) | Same platform as API — one bill of materials; free tier |
| React App | **Vercel** | Best-in-class static hosting; zero-config React deploy; free custom domain |
| Flutter APK | **GitHub Releases** | Free; version-controlled; stable URL for submission |
| Demo video | **YouTube (unlisted)** | Required by spec (Section 15); free; link-sharing compliant |

### Deployment topology
User browser
│
▼
Vercel (React) ─── HTTPS ───► Render (ASP.NET Core API)
│
├─► Render PostgreSQL
│
└─► Render (Python AI Service)
│
└─► back to ASP.NET Core API
(internal HTTP)

### Environment variables (Render dashboard)

| Key | Where used | Value |
|-----|-----------|-------|
| `ConnectionStrings__EventCrew` | ASP.NET Core | Render Postgres internal URL |
| `AiService__BaseUrl` | ASP.NET Core | `https://<ai-service>.onrender.com` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core | `Production` |

Secrets are set via Render's environment panel — **never committed to Git**.
The Python service consumes API-authorized event/venue snapshots and requires no backend URL or authentication credential. Keep it private to the ASP.NET Core API.

### Cost & availability strategy

- All services run on free tiers; no paid subscription required
- Render free Web Services sleep after 15 min of inactivity → first request may take ~30s
- For final demonstration, we will **warm the API and AI service** beforehand (10 min in advance)
- Free Postgres lasts 90 days from creation — sufficient for the assignment timeline (due 30 Sept 2026)
- If any service experiences an outage during evaluation, we provide evidence per Section 14

### Fallback plan

If Render's free tier changes before deployment:
- **Plan B:** Fly.io for API + Postgres (still free)
- **Plan C:** Local Docker Compose run with ngrok for public URL (documented in submission)

## Consequences

**Positive:**
- Zero cost
- Deployable in hours, not days
- Modern DX (push to deploy)
- Postgres managed (no manual backup/patching)
- Matches our local Docker workflow — same Dockerfile deployable locally and on Render

**Negative:**
- Free Web Services sleep → cold starts
- Render Postgres 90-day free window; must be provisioned close to submission
- Some platform-specific configuration (`render.yaml`, Dockerfile tuning)

**Neutral:**
- The deployment topology is decoupled: switching to a different cloud is a matter of re-pointing URLs, no code changes
- ASP.NET Core's DI + env-var config pattern means no hardcoded values

## References

- Render docs: https://render.com/docs
- Vercel docs: https://vercel.com/docs
- Local setup: `docker-compose.yml` + `README.md`
