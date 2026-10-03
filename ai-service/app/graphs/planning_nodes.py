"""
Node functions for the planning graph.

Each node:
- receives the current PlanningState
- does one focused thing
- returns a partial update (dict) that the graph merges into the state
"""

import time
from datetime import datetime, timezone

from app.graphs.planning_state import PlanningState
from app.tools.event_tools import get_event
from app.tools.venue_tools import get_venue
from app.tools.scheduling_tools import calculate_staffing_ratio


# ---------------------------------------------------------------------------
# Helper for building tool-call log entries
# ---------------------------------------------------------------------------
def _log_tool_call(tool_name: str, input_params: dict, output_summary: str, duration_ms: int) -> dict:
    return {
        "tool_name": tool_name,
        "input_params": input_params,
        "output_summary": output_summary,
        "duration_ms": duration_ms,
        "called_at": datetime.now(timezone.utc).isoformat(),
    }


# ---------------------------------------------------------------------------
# Node 1: Fetch the event
# ---------------------------------------------------------------------------
async def fetch_event_node(state: PlanningState) -> dict:
    """Inspect the API-authorized event snapshot. Sets state.event."""
    t0 = time.perf_counter()
    event = await get_event(state.event_id, state.event_context)
    duration_ms = int((time.perf_counter() - t0) * 1000)

    if event is None:
        return {
            "error": f"Event '{state.event_id}' not found.",
            "status": "failed",
        }

    log_entry = _log_tool_call(
        tool_name="get_event",
        input_params={"event_id": state.event_id},
        output_summary=f"Retrieved event '{event.title}' ({len(event.role_requirements)} roles)",
        duration_ms=duration_ms,
    )

    step_entry = {
        "step_number": 1,
        "action": f"Fetch event details for '{event.title}'",
        "tool": "get_event",
        "agent": "PlanningAgent",
        "status": "planned",
    }

    return {
        "event": event,
        "tool_calls": state.tool_calls + [log_entry],
        "steps": state.steps + [step_entry],
    }


# ---------------------------------------------------------------------------
# Node 2: Fetch the venue
# ---------------------------------------------------------------------------
async def fetch_venue_node(state: PlanningState) -> dict:
    """Inspect the API-authorized venue snapshot. Sets state.venue."""
    if state.event is None:
        return {"error": "Cannot fetch venue without an event.", "status": "failed"}

    if state.event.venue_id is None:
        return {
            "error": f"Event '{state.event.title}' has no venue assigned.",
            "status": "failed",
        }

    t0 = time.perf_counter()
    venue = await get_venue(state.event.venue_id, state.venue_context)
    duration_ms = int((time.perf_counter() - t0) * 1000)

    if venue is None:
        return {
            "error": f"Venue '{state.event.venue_id}' not found.",
            "status": "failed",
        }

    log_entry = _log_tool_call(
        tool_name="get_venue",
        input_params={"venue_id": state.event.venue_id},
        output_summary=f"Retrieved venue '{venue.name}' (capacity {venue.capacity})",
        duration_ms=duration_ms,
    )

    step_entry = {
        "step_number": 2,
        "action": f"Fetch venue '{venue.name}' capacity",
        "tool": "get_venue",
        "agent": "PlanningAgent",
        "status": "planned",
    }

    return {
        "venue": venue,
        "tool_calls": state.tool_calls + [log_entry],
        "steps": state.steps + [step_entry],
    }


# ---------------------------------------------------------------------------
# Node 3: Calculate staffing ratio
# ---------------------------------------------------------------------------
async def calculate_ratio_node(state: PlanningState) -> dict:
    """Compute the recommended staffing ratio. Sets state.ratio."""
    if state.venue is None:
        return {"error": "Cannot calculate ratio without a venue.", "status": "failed"}

    t0 = time.perf_counter()
    ratio = calculate_staffing_ratio(state.venue.capacity)
    duration_ms = int((time.perf_counter() - t0) * 1000)

    log_entry = _log_tool_call(
        tool_name="calculate_staffing_ratio",
        input_params={"venue_capacity": state.venue.capacity},
        output_summary=ratio.reasoning,
        duration_ms=duration_ms,
    )

    step_entry = {
        "step_number": 3,
        "action": f"Calculate staffing ratio ({ratio.total_staff} total staff recommended)",
        "tool": "calculate_staffing_ratio",
        "agent": "PlanningAgent",
        "status": "planned",
    }

    return {
        "ratio": ratio,
        "tool_calls": state.tool_calls + [log_entry],
        "steps": state.steps + [step_entry],
    }


# ---------------------------------------------------------------------------
# Node 4: Build the final plan
# ---------------------------------------------------------------------------
async def build_plan_node(state: PlanningState) -> dict:
    """Assemble the final plan. Sets state.objective, state.reasoning, etc."""
    if state.event is None or state.venue is None or state.ratio is None:
        return {"error": "Cannot build plan without event, venue and ratio.", "status": "failed"}

    staffing_recommendations = [
        {
            "role_name": role.role_name,
            "required_headcount": role.required_headcount,
            "minimum_experience_level": role.min_experience_level,
        }
        for role in state.event.role_requirements
    ]
    configured_headcount = sum(role.required_headcount for role in state.event.role_requirements)
    next_agent = "OrganizerReview"

    step_entry = {
        "step_number": 4,
        "action": (
            f"Prepare the proposal for organizer review: "
            f"{len(staffing_recommendations)} configured roles, "
            f"{configured_headcount} required volunteers."
        ),
        "tool": None,
        "agent": next_agent,
        "status": "planned",
    }

    reasoning = (
        f"Event '{state.event.title}' at '{state.venue.name}' "
        f"(capacity {state.venue.capacity}). The capacity-based baseline is "
        f"{state.ratio.recommended_ushers} ushers and "
        f"{state.ratio.recommended_registration_staff} registration staff are recommended "
        f"(total {state.ratio.total_staff}). The proposal preserves "
        f"{configured_headcount} volunteers across {len(staffing_recommendations)} "
        "configured event roles; it does not assign individual volunteers."
    )

    return {
        "objective": f"Plan staffing for event '{state.event.title}'",
        "reasoning": reasoning,
        "steps": state.steps + [step_entry],
        "next_agent": next_agent,
        "staffing_recommendations": staffing_recommendations,
        "status": "planned",
    }