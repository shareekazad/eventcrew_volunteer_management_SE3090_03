"""Node functions for the event scheduling graph."""

import time
from datetime import datetime, timezone

from app.graphs.planning_state import PlanningState
from app.tools.event_tools import get_event
from app.tools.venue_tools import get_venue
from app.tools.scheduling_tools import build_roster, calculate_staffing_ratio


def _log_tool_call(tool_name: str, input_params: dict, output_summary: str, duration_ms: int) -> dict:
    return {
        "tool_name": tool_name,
        "input_params": input_params,
        "output_summary": output_summary,
        "duration_ms": duration_ms,
        "called_at": datetime.now(timezone.utc).isoformat(),
    }


async def fetch_event_node(state: PlanningState) -> dict:
    t0 = time.perf_counter()
    event = await get_event(state.event_id, state.event_context)
    duration_ms = int((time.perf_counter() - t0) * 1000)

    if event is None:
        return {"error": f"Event '{state.event_id}' not found.", "status": "failed"}

    log_entry = _log_tool_call(
        tool_name="get_event",
        input_params={"event_id": state.event_id},
        output_summary=f"Retrieved event '{event.title}' ({len(event.role_requirements)} roles)",
        duration_ms=duration_ms,
    )

    return {
        "event": event,
        "tool_calls": state.tool_calls + [log_entry],
        "steps": state.steps + [{
            "step_number": 1,
            "action": f"Fetch event details for '{event.title}'",
            "tool": "get_event",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def fetch_venue_node(state: PlanningState) -> dict:
    if state.event is None:
        return {"error": "Cannot fetch venue without an event.", "status": "failed"}
    if state.event.venue_id is None:
        return {"error": f"Event '{state.event.title}' has no venue assigned.", "status": "failed"}

    t0 = time.perf_counter()
    venue = await get_venue(state.event.venue_id, state.venue_context)
    duration_ms = int((time.perf_counter() - t0) * 1000)

    if venue is None:
        return {"error": f"Venue '{state.event.venue_id}' not found.", "status": "failed"}

    log_entry = _log_tool_call(
        tool_name="get_venue",
        input_params={"venue_id": state.event.venue_id},
        output_summary=f"Retrieved venue '{venue.name}' (capacity {venue.capacity})",
        duration_ms=duration_ms,
    )

    return {
        "venue": venue,
        "tool_calls": state.tool_calls + [log_entry],
        "steps": state.steps + [{
            "step_number": 2,
            "action": f"Fetch venue '{venue.name}' capacity",
            "tool": "get_venue",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def calculate_ratio_node(state: PlanningState) -> dict:
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

    return {
        "ratio": ratio,
        "tool_calls": state.tool_calls + [log_entry],
        "steps": state.steps + [{
            "step_number": 3,
            "action": f"Calculate staffing ratio ({ratio.total_staff} total staff recommended)",
            "tool": "calculate_staffing_ratio",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def load_shifts_node(state: PlanningState) -> dict:
    if state.event is None:
        return {"error": "Cannot load shifts without an event.", "status": "failed"}

    role_requirements = getattr(state.event, "role_requirements", [])
    shifts = []
    for role in role_requirements:
        shifts.append({
            "id": str(role.id),
            "event_id": state.event.id,
            "role_requirement_id": role.id,
            "title": role.role_name,
            "role_name": role.role_name,
            "start_time": state.event.start_date,
            "end_time": state.event.end_date,
            "capacity": max(1, role.required_headcount),
            "status": "Scheduled",
        })

    return {
        "shifts": shifts,
        "tool_calls": state.tool_calls + [_log_tool_call(
            "load_shift_requirements",
            {"event_id": state.event.id},
            f"Loaded {len(shifts)} shift requirements for '{state.event.title}'",
            0,
        )],
        "steps": state.steps + [{
            "step_number": 4,
            "action": f"Load shift requirements for '{state.event.title}'",
            "tool": "load_shift_requirements",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def load_volunteers_node(state: PlanningState) -> dict:
    volunteers = state.volunteers if state.volunteers else [
        {
            "id": "volunteer-1",
            "full_name": "Auto Assigned Volunteer",
            "email": "volunteer@example.com",
            "is_active": True,
            "max_hours": 8,
            "role_preferences": [],
            "eligible_roles": [],
            "availability": [{"start": state.event.start_date if state.event else "2026-01-01T09:00:00+00:00", "end": state.event.end_date if state.event else "2026-01-01T17:00:00+00:00"}],
        }
    ]

    return {
        "volunteers": volunteers,
        "tool_calls": state.tool_calls + [_log_tool_call(
            "load_volunteers",
            {"count": len(volunteers)},
            f"Loaded {len(volunteers)} volunteers for scheduling",
            0,
        )],
        "steps": state.steps + [{
            "step_number": 5,
            "action": "Load volunteer availability and maximum hours",
            "tool": "load_volunteers",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def build_schedule_node(state: PlanningState) -> dict:
    if state.event is None:
        return {"error": "Cannot build schedule without an event.", "status": "failed"}

    roster, unfilled_slots, validation = build_roster(
        event=state.event,
        shifts=state.shifts,
        volunteers=state.volunteers,
    )

    return {
        "roster": roster,
        "unfilled_slots": unfilled_slots,
        "validation": validation,
        "tool_calls": state.tool_calls + [_log_tool_call(
            "build_schedule",
            {"shifts": len(state.shifts), "volunteers": len(state.volunteers)},
            f"Built roster with {len(roster)} assignments and {len(unfilled_slots)} unfilled slots",
            0,
        )],
        "steps": state.steps + [{
            "step_number": 6,
            "action": "Assign volunteers to shift slots using constraints",
            "tool": "build_schedule",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def validate_schedule_node(state: PlanningState) -> dict:
    validation = state.validation or {
        "availability_violations": 0,
        "overlap_violations": 0,
        "hour_limit_violations": 0,
        "role_requirement_violations": 0,
        "total_assigned_volunteers": 0,
        "total_unfilled_slots": 0,
        "is_valid": True,
    }
    return {
        "validation": validation,
        "tool_calls": state.tool_calls + [_log_tool_call(
            "validate_schedule",
            {"roster_count": len(state.roster)},
            f"Validation complete: {validation['is_valid']}",
            0,
        )],
        "steps": state.steps + [{
            "step_number": 7,
            "action": "Validate the generated roster against hard constraints",
            "tool": "validate_schedule",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def generate_roster_node(state: PlanningState) -> dict:
    roster = state.roster or []
    unfilled_slots = state.unfilled_slots or []
    validation = state.validation or {
        "availability_violations": 0,
        "overlap_violations": 0,
        "hour_limit_violations": 0,
        "role_requirement_violations": 0,
        "total_assigned_volunteers": 0,
        "total_unfilled_slots": 0,
        "is_valid": True,
    }

    return {
        "roster": roster,
        "unfilled_slots": unfilled_slots,
        "validation": validation,
        "tool_calls": state.tool_calls + [_log_tool_call(
            "generate_roster",
            {"assignments": len(roster), "unfilled_slots": len(unfilled_slots)},
            f"Generated roster JSON with {len(roster)} assignments",
            0,
        )],
        "steps": state.steps + [{
            "step_number": 8,
            "action": "Generate the final roster JSON for organizer review",
            "tool": "generate_roster",
            "agent": "PlanningAgent",
            "status": "planned",
        }],
    }


async def build_plan_node(state: PlanningState) -> dict:
    if state.event is None or state.venue is None or state.ratio is None:
        return {"error": "Cannot build plan without event, venue and ratio.", "status": "failed"}

    staffing_recommendations = [{
        "role_name": role.role_name,
        "required_headcount": role.required_headcount,
        "minimum_experience_level": role.min_experience_level,
    } for role in state.event.role_requirements]

    next_agent = "OrganizerReview"
    summary = (
        f"Event '{state.event.title}' at '{state.venue.name}' has a final roster of "
        f"{len(state.roster)} assignments and {len(state.unfilled_slots)} unfilled slots. "
        f"The generated schedule is valid: {state.validation.get('is_valid', False) if state.validation else False}."
    )

    return {
        "objective": f"Schedule volunteers for event '{state.event.title}'",
        "reasoning": summary,
        "steps": state.steps + [{
            "step_number": 9,
            "action": "Prepare roster for organizer review",
            "tool": None,
            "agent": next_agent,
            "status": "planned",
        }],
        "next_agent": next_agent,
        "staffing_recommendations": staffing_recommendations,
        "status": "planned",
    }