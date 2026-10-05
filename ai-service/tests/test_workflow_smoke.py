"""
Smoke test for the unified multi-agent workflow.

Run with:
    python -m tests.test_workflow_smoke

Requires:
    - ASP.NET Core API running on port 5100
    - Postgres container running (docker compose up -d)
    - Event ID below must exist in the DB with at least 1 role requirement
"""

import asyncio

from app.graphs.workflow_graph import run_workflow


# ---------------------------------------------------------------------------
# Change this to a real event ID from your DB if needed
# ---------------------------------------------------------------------------
EVENT_ID = "f19b35ea-0355-4b95-acac-b58c820dbc3c"


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
def _hr(title: str = "") -> None:
    """Print a horizontal separator, optionally with a title."""
    print("─" * 72)
    if title:
        print(title)
        print("─" * 72)


def _safe_get(d: dict | None, *keys, default="—"):
    """Nested-safe dict getter."""
    cur = d
    for k in keys:
        if not isinstance(cur, dict) or k not in cur:
            return default
        cur = cur[k]
    return cur


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
async def main() -> None:
    print("=" * 72)
    print(f"Running unified workflow for event: {EVENT_ID}")
    print("=" * 72)
    print()

    try:
        result = await run_workflow(EVENT_ID)
    except ValueError as e:
        print(f"❌ WORKFLOW FAILED: {e}")
        return
    except Exception as e:
        print(f"❌ UNEXPECTED ERROR: {type(e).__name__}: {e}")
        return

    # ---- Status ----
    print(f"Status:              {result.get('status')}")
    print(f"Workflow ID:         {result.get('workflow_id')}")
    print(f"Current step:        {result.get('current_step')}")
    print(f"Error:               {result.get('error') or '—'}")
    print()

    # ---- Node 1: Planning ----
    _hr("NODE 1: PLANNING AGENT")
    print(f"  Event:     {_safe_get(result, 'event', 'title')}")
    print(f"  Venue:     {_safe_get(result, 'venue', 'name')} "
          f"(capacity {_safe_get(result, 'venue', 'capacity')})")
    print(f"  Ratio:     {_safe_get(result, 'staffing_ratio', 'total_staff')} total staff")
    print(f"  Reasoning: {result.get('plan_reasoning', '—')}")
    print()

    # ---- Node 2: Matching ----
    _hr("NODE 2: MATCHING AGENT")
    print(f"  Total matched:   {result.get('total_matched', 0)}")
    print(f"  Total needed:    {result.get('total_headcount_needed', 0)}")
    matching_results = result.get("matching_results", [])
    if matching_results:
        for mr in matching_results:
            matched = len(mr.get("matched_candidates", []))
            status = mr.get("status", "?")
            role = mr.get("role_name", "?")
            print(f"    - '{role}': {status} ({matched} matched)")
    else:
        print("    (no matching results)")
    print()

    # ---- Node 3: Scheduling ----
    _hr("NODE 3: SCHEDULING AGENT")
    shifts = result.get("proposed_shifts", [])
    conflicts = result.get("shift_conflicts", [])
    print(f"  Shifts proposed:  {len(shifts)}")
    print(f"  Conflicts found:  {len(conflicts)}")
    for s in shifts[:5]:
        assigned = len(s.get("assigned_candidates", []))
        start = s.get("start_time", "")[:16]
        end = s.get("end_time", "")[:16]
        print(f"    - {s.get('role_name', '?'):<25} {assigned} assigned  {start} → {end}")
    if len(shifts) > 5:
        print(f"    ... and {len(shifts) - 5} more")
    if conflicts:
        print("  Conflict detail:")
        for c in conflicts[:5]:
            print(f"    - [{c.get('severity', '?'):<6}] {c.get('message', '?')}")
    print()

    # ---- Node 4: Validation ----
    _hr("NODE 4: VALIDATION AGENT")
    passed = result.get("validation_passed", False)
    errors = result.get("validation_errors", [])
    warnings = result.get("validation_warnings", [])
    print(f"  Validation passed: {passed}")
    print(f"  Errors:            {len(errors)}")
    print(f"  Warnings:          {len(warnings)}")
    if errors:
        print("  Errors:")
        for err in errors[:5]:
            print(f"    ❌ {err}")
    if warnings:
        print("  Warnings:")
        for warn in warnings[:5]:
            print(f"    ⚠️  {warn}")
    print()

    # ---- Agent traces (audit trail) ----
    traces = result.get("agent_traces", [])
    _hr(f"AGENT TRACES ({len(traces)} entries)")
    for t in traces:
        agent = t.get("agent_name", "?")
        tool = t.get("tool_name", "?")
        dur = t.get("duration_ms", 0)
        print(f"  [{agent:<25}] {tool:<30} {dur:>5} ms")
    print()

    # ---- Summary ----
    _hr("SUMMARY")
    agents = sorted({t.get("agent_name", "?") for t in traces})
    print(f"  Distinct agents that ran:  {len(agents)}")
    for a in agents:
        print(f"    ✅ {a}")
    print()
    print(f"  Total tool calls:          {len(traces)}")
    print(f"  Workflow completed:        {result.get('status') == 'running' or passed}")


if __name__ == "__main__":
    asyncio.run(main())