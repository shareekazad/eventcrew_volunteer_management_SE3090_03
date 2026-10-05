"""
Allow-listed tools for the SchedulingAgent.

Implements 3 tools (Section 9.1):
1. fetch_event_shifts    — existing shifts from the backend
2. propose_shift_slots   — deterministic shift-slot proposal
3. check_shift_conflicts — internal consistency check on the proposal
"""

import logging
import time
import uuid
from datetime import datetime, timedelta, timezone
from typing import Any

from app.tools.http_client import BackendClient, BackendError
from app.tools.matching_tools import log_agent_observability


logger = logging.getLogger("eventcrew-ai.scheduling_agent_tools")

# Section 9.1 allow-list for the SchedulingAgent
SCHEDULING_ALLOWED_TOOLS = {
    "fetch_event_shifts",
    "propose_shift_slots",
    "check_shift_conflicts",
    "log_agent_observability",
}


# ---------------------------------------------------------------------------
# Tool 1: fetch_event_shifts
# ---------------------------------------------------------------------------
async def fetch_event_shifts(event_id: str) -> list[dict]:
    """
    Retrieves all existing shifts for an event from the backend.
    """
    try:
        async with BackendClient() as client:
            data = await client.get(f"/api/Events/{event_id}/shifts")
            if not data or not isinstance(data, list):
                return []
            return data
    except (BackendError, Exception) as exc:
        logger.warning(
            "Could not fetch existing shifts for event %s (fallback to empty): %s",
            event_id, exc
        )
        return []


# ---------------------------------------------------------------------------
# Tool 2: propose_shift_slots
# ---------------------------------------------------------------------------
def propose_shift_slots(
    event_start_iso: str,
    event_end_iso: str,
    role_requirements: list[dict],
    matching_results: list[dict],
) -> list[dict]:
    """
    Deterministic shift-slot proposal.

    Splits the event into N equal shifts per role, assigns candidates
    round-robin, respects the max 6-hour shift rule.
    """
    if not role_requirements:
        return []

    event_start = datetime.fromisoformat(event_start_iso.replace("Z", "+00:00"))
    event_end = datetime.fromisoformat(event_end_iso.replace("Z", "+00:00"))
    total_duration = event_end - event_start

    MAX_SHIFT_HOURS = 6
    proposed: list[dict] = []

    for role in role_requirements:
        role_name = role.get("roleName") or role.get("role_name") or "Unknown Role"
        role_id = role.get("id")
        headcount = int(role.get("requiredHeadcount") or role.get("required_headcount") or 1)

        match_data = next(
            (m for m in matching_results if m.get("role_name") == role_name),
            None,
        )
        candidates = match_data.get("matched_candidates", []) if match_data else []

        num_shifts = 2 if total_duration > timedelta(hours=MAX_SHIFT_HOURS) else 1
        shift_duration = total_duration / num_shifts

        if shift_duration > timedelta(hours=MAX_SHIFT_HOURS):
            shift_duration = timedelta(hours=MAX_SHIFT_HOURS)

        per_shift = max(1, (len(candidates) + num_shifts - 1) // num_shifts)

        for shift_idx in range(num_shifts):
            shift_start = event_start + (shift_duration * shift_idx)
            shift_end = shift_start + shift_duration

            slice_start = shift_idx * per_shift
            slice_end = slice_start + per_shift
            assigned = candidates[slice_start:slice_end]

            proposed.append({
                "shift_id": str(uuid.uuid4()),
                "role_name": role_name,
                "role_requirement_id": role_id,
                "start_time": shift_start.isoformat(),
                "end_time": shift_end.isoformat(),
                "capacity": min(headcount, len(assigned)) or headcount,
                "assigned_candidates": assigned,
                "reasoning": (
                    f"Shift {shift_idx + 1}/{num_shifts} for {role_name}: "
                    f"{len(assigned)} candidate(s) assigned. "
                    f"Slot spans {shift_duration.total_seconds() / 3600:.1f}h."
                ),
            })

    return proposed


# ---------------------------------------------------------------------------
# Tool 3: check_shift_conflicts
# ---------------------------------------------------------------------------
def check_shift_conflicts(proposed_shifts: list[dict]) -> list[dict]:
    """
    Detects conflicts in the proposed shift roster.

    1. A candidate assigned to two overlapping shifts
    2. A shift whose length exceeds 6 hours
    3. A shift assigned fewer candidates than capacity
    """
    conflicts: list[dict] = []

    candidate_schedule: dict[str, list[tuple[datetime, datetime]]] = {}

    for shift in proposed_shifts:
        start = datetime.fromisoformat(shift["start_time"])
        end = datetime.fromisoformat(shift["end_time"])

        for cand in shift.get("assigned_candidates", []):
            vol_id = str(cand.get("volunteer_id") or cand.get("volunteerId") or "")
            if not vol_id:
                continue

            intervals = candidate_schedule.setdefault(vol_id, [])
            for (existing_start, existing_end) in intervals:
                if start < existing_end and end > existing_start:
                    conflicts.append({
                        "type": "double_booking",
                        "severity": "high",
                        "message": (
                            f"Candidate {cand.get('volunteer_name', vol_id)} "
                            f"is booked on overlapping shifts."
                        ),
                        "affected_ids": [vol_id, shift["shift_id"]],
                    })
            intervals.append((start, end))

    for shift in proposed_shifts:
        start = datetime.fromisoformat(shift["start_time"])
        end = datetime.fromisoformat(shift["end_time"])
        hours = (end - start).total_seconds() / 3600
        if hours > 6.0:
            conflicts.append({
                "type": "shift_too_long",
                "severity": "medium",
                "message": f"Shift {shift['shift_id'][:8]} exceeds 6 hours ({hours:.1f}h).",
                "affected_ids": [shift["shift_id"]],
            })

    for shift in proposed_shifts:
        assigned = len(shift.get("assigned_candidates", []))
        capacity = shift.get("capacity", 0)
        if assigned < capacity:
            conflicts.append({
                "type": "understaffed",
                "severity": "low",
                "message": (
                    f"Shift {shift['shift_id'][:8]} for '{shift['role_name']}' "
                    f"has {assigned}/{capacity} slots filled."
                ),
                "affected_ids": [shift["shift_id"]],
            })

    return conflicts