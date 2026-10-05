# ADR-001: React State Management — Zustand

- **Date:** 2026-09-23
- **Status:** Accepted
- **Deciders:** The Team (IT24104324, IT24102554, IT24102868, IT24103013)

## Context

The EventCrew React web application handles:
- Demo role state (Organizer or Volunteer, stored locally for navigation only)
- Server data (events, venues, applicants, roster proposals)
- UI state (wizard step, modal visibility, form drafts)
- Cross-cutting state (notifications, loading flags)

We need a state-management approach that:
- Is simple enough for a 4-person team across 9 weeks
- Supports async server calls cleanly
- Scales without boilerplate
- Doesn't force a rigid pattern on every developer

## Options Considered

### 1. React Context + useReducer
- ✅ Built-in — no dependency
- ✅ Fine for lightweight demo role state
- ❌ Manual re-render optimization (contexts re-render every consumer)
- ❌ Doesn't handle async out-of-the-box
- ❌ Verbose for cross-cutting concerns (loading, errors)

### 2. Redux Toolkit (RTK)
- ✅ Industry-standard, well-documented
- ✅ Excellent devtools + time-travel debugging
- ✅ RTK Query simplifies server data
- ❌ Significant boilerplate (slices, thunks, store setup)
- ❌ Learning curve for a team new to it
- ❌ Overkill for our scope

### 3. Zustand
- ✅ Minimal boilerplate — a store is just a hook
- ✅ Async actions built-in (no thunks needed)
- ✅ Selective subscriptions → no unnecessary re-renders
- ✅ Tiny (~1 KB), zero dependencies
- ✅ Plays well with React Query if we add it later
- ⚠️ Fewer devtools than Redux (but a devtools middleware exists)
- ⚠️ Smaller ecosystem

## Decision

**Use Zustand for global client state**, with plain `fetch` + `useEffect` for server data in the initial scope.

Specifically:
- One store per **domain** (`demoRoleStore`, `eventStore`, `uiStore`)
- Server data fetched via small service modules (`api/events.ts`), not stored globally unless shared across routes
- If server data needs more sophistication later, we adopt **TanStack Query** alongside Zustand (they're designed to work together)

## Consequences

**Positive:**
- Fast onboarding for teammates — Zustand's API is ~5 lines to understand
- Less code to review, less to break
- Rerender performance is good by default

**Negative:**
- No time-travel debugging (acceptable trade-off)
- We must enforce naming conventions manually (no enforced structure like Redux slices)

**Neutral:**
- Migration path to TanStack Query exists if server data complexity grows

## References

- Zustand documentation: https://docs.pmnd.rs/zustand
