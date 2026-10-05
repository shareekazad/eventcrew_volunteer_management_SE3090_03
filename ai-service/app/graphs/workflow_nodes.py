"""
Node functions for the unified multi-agent workflow.

Each node:
- receives the current WorkflowState
- calls its agent
- returns a partial state update

Nodes in order:
    plan_node → match_node → schedule_node → validate_node
"""

import time
import uuid
from datetime import datetime, timezone

from app.graphs.workflow_state import WorkflowState
from app.agents.planning_agent import PlanningAgent
from app.agents.matching_agent import VolunteerMatchingAgent
from app.agents.scheduling_agent import SchedulingAgent
from app.agents.validation_agent import ValidationAgent
from app.schemas.matching_schema import MatchingRequest
from app.tools.event_tools import get_event
from app.tools.venue_tools import get_venue
from app.tools.scheduling_tools import calculate_staffing_ratio


# ===========================================================================
# Node 1: PLANNING
# ===========================================================================
async def plan_node(state: WorkflowState) -> dict:
    """
    Node 1 — Runs the PlanningAgent.

    Fetches event + venue, computes staffing ratio, produces a structured plan.
    Populates: event, venue, staffing_ratio, plan_steps, plan_reasoning
    """
    workflow_id = state.workflow_id or str(uuid.uuid4())
    traces = list(state.agent_traces)

    # Fetch event
    t = time.perf_counter()
    event = await get_event(state.event_id)
    duration_ms = int((time.perf_counter() - t) * 1000)

    if event is None:
        return {
            "error": f"Event '{state.event_id}' not found.",
            "status": "failed",
            "workflow_id": workflow_id,
        }

    traces.append({
        "agent_name": "PlanningAgent",
        "tool_name": "get_event",
        "input_params": {"event_id": state.event_id},
        "output_summary": {
            "title": event.title,
            "roles": len(event.role_requirements),
        },
        "duration_ms": duration_ms,
        "called_at": _now_iso(),
    })

    if event.venue_id is None:
        return {
            "event": event,
            "error": f"Event '{event.title}' has no venue assigned.",
            "status": "failed",
            "workflow_id": workflow_id,
            "agent_traces": traces,
        }

    # Fetch venue
    t = time.perf_counter()
    venue = await get_venue(event.venue_id)
    duration_ms = int((time.perf_counter() - t) * 1000)

    if venue is None:
        return {
            "event": event,
            "error": f"Venue '{event.venue_id}' not found.",
            "status": "failed",
            "workflow_id": workflow_id,
            "agent_traces": traces,
        }

    traces.append({
        "agent_name": "PlanningAgent",
        "tool_name": "get_venue",
        "input_params": {"venue_id": event.venue_id},
        "output_summary": {"name": venue.name, "capacity": venue.capacity},
        "duration_ms": duration_ms,
        "called_at": _now_iso(),
    })

    # Compute staffing ratio
    t = time.perf_counter()
    ratio = calculate_staffing_ratio(venue.capacity)
    duration_ms = int((time.perf_counter() - t) * 1000)

    traces.append({
        "agent_name": "PlanningAgent",
        "tool_name": "calculate_staffing_ratio",
        "input_params": {"venue_capacity": venue.capacity},
        "output_summary": {
            "ushers": ratio.recommended_ushers,
            "registration": ratio.recommended_registration_staff,
        },
        "duration_ms": duration_ms,
        "called_at": _now_iso(),
    })

    # Build plan steps (same structure as before)
    plan_steps = [
        {"step_number": 1, "action": f"Fetch event '{event.title}'",
         "tool": "get_event", "agent": "PlanningAgent", "status": "planned"},
        {"step_number": 2, "action": f"Fetch venue '{venue.name}' capacity",
         "tool": "get_venue", "agent": "PlanningAgent", "status": "planned"},
        {"step_number": 3, "action": f"Calculate staffing ratio ({ratio.total_staff} total)",
         "tool": "calculate_staffing_ratio", "agent": "PlanningAgent", "status": "planned"},
        {"step_number": 4, "action": "Delegate to MatchingAgent",
         "tool": None, "agent": "MatchingAgent", "status": "planned"},
    ]

    reasoning = (
        f"Event '{event.title}' at '{venue.name}' (capacity {venue.capacity}). "
        f"Recommended: {ratio.recommended_ushers} ushers and "
        f"{ratio.recommended_registration_staff} registration staff "
        f"(total {ratio.total_staff}). Delegating to MatchingAgent."
    )

    return {
        "workflow_id": workflow_id,
        "event": event,
        "venue": venue,
        "staffing_ratio": ratio,
        "plan_steps": plan_steps,
        "plan_reasoning": reasoning,
        "agent_traces": traces,
        "current_step": 1,
    }


# ===========================================================================
# Node 2: MATCHING
# ===========================================================================
async def match_node(state: WorkflowState) -> dict:
    """
    Node 2 — Runs the MatchingAgent for each role requirement.

    For each role in the event, calls the agent to rank candidates.
    Populates: matching_results (one entry per role)
    """
    traces = list(state.agent_traces)
    agent = VolunteerMatchingAgent()

    if state.event is None:
        return {
            "error": "match_node: missing event in state.",
            "status": "failed",
        }

    matching_results: list[dict] = []
    total_matched = 0
    total_needed = 0

    # Iterate over each role requirement
    for role in state.event.role_requirements:
        t = time.perf_counter()

        # Build matching request for this role
        # Note: roles don't have required_skills in our schema yet —
        # pass empty list, agent handles it
        request = MatchingRequest(
            event_id=state.event.id,
            role_name=role.role_name,
            required_skills=[],
            min_experience_level=role.min_experience_level,
            required_headcount=role.required_headcount,
        )

        try:
            result = await agent.match(request)
            duration_ms = int((time.perf_counter() - t) * 1000)

            result_dict = result.model_dump(mode="json")
            matching_results.append(result_dict)
            total_matched += len(result.matched_candidates)
            total_needed += role.required_headcount

            traces.append({
                "agent_name": "VolunteerMatchingAgent",
                "tool_name": "match",
                "input_params": {
                    "role_name": role.role_name,
                    "headcount": role.required_headcount,
                },
                "output_summary": {
                    "status": result.status,
                    "matched": len(result.matched_candidates),
                    "unfulfilled": result.unfulfilled_slots,
                },
                "duration_ms": duration_ms,
                "called_at": _now_iso(),
            })

        except Exception as exc:
            duration_ms = int((time.perf_counter() - t) * 1000)
            traces.append({
                "agent_name": "VolunteerMatchingAgent",
                "tool_name": "match",
                "input_params": {"role_name": role.role_name},
                "output_summary": {"error": str(exc)},
                "duration_ms": duration_ms,
                "called_at": _now_iso(),
            })
            # Matching failure doesn't fail the whole workflow —
            # we record it and continue with empty matches for this role
            matching_results.append({
                "role_name": role.role_name,
                "status": "SAFE_FAILURE",
                "matched_candidates": [],
                "unfulfilled_slots": role.required_headcount,
                "error": str(exc),
            })

    return {
        "matching_results": matching_results,
        "total_matched": total_matched,
        "total_headcount_needed": total_needed,
        "agent_traces": traces,
        "current_step": 2,
    }


# ===========================================================================
# Node 3: SCHEDULING
# ===========================================================================
async def schedule_node(state: WorkflowState) -> dict:
    """
    Node 3 — Runs the SchedulingAgent.

    Populates: proposed_shifts, shift_conflicts
    """
    agent = SchedulingAgent()
    result = await agent.schedule(state)
    result["current_step"] = 3
    return result


# ===========================================================================
# Node 4: VALIDATION
# ===========================================================================
async def validate_node(state: WorkflowState) -> dict:
    """
    Node 4 — Runs the ValidationAgent.

    Populates: validation_passed, validation_errors, validation_warnings
    """
    agent = ValidationAgent()
    result = await agent.validate(state)
    result["current_step"] = 4
    return result


# ===========================================================================
# Helpers
# ===========================================================================
def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()