"""
Allow-listed tools for the ValidationAgent — Student 4's agent slot.

⚠️ NOTE ON OWNERSHIP:
Scaffolded by Student 1 - IT24104324, to unblock the group workflow.
Student 4 owns this file — please extend with:
  - Additional validation rules (e.g., venue capacity checks, skill coverage minimums)
  - More detailed error categorization
  - Unit tests

The framework (state, node wiring, orchestration) is shared and stable.
"""
"""
Deterministic validation tools for the ValidationAgent.

Validation rules implemented:
1. assert_no_double_booking  — no candidate is booked on overlapping shifts
2. assert_headcount_met      — every role's headcount requirement is satisfied
3. assert_experience_match   — every assigned candidate meets min experience tier
4. assert_shift_duration     — no shift exceeds 6 hours
5. assert_event_bounds       — all shifts fall inside the event window
"""

import logging
from datetime import datetime

from app.tools.matching_tools import EXPERIENCE_TIERS
from app.graphs.workflow_state import WorkflowState


logger = logging.getLogger("eventcrew-ai.validation_tools")

# Section 9.1 allow-list
VALIDATION_ALLOWED_TOOLS = {
    "assert_no_double_booking",
    "assert_headcount_met",
    "assert_experience_match",
    "assert_shift_duration",
    "assert_event_bounds",
    "log_agent_observability",
}


# ---------------------------------------------------------------------------
# Rule 1: no double-booking
# ---------------------------------------------------------------------------
def assert_no_double_booking(proposed_shifts: list[dict]) -> list[str]:
    """
    Checks that no candidate is scheduled on overlapping shifts.

    Returns:
        List of error messages (empty = passes).
    """
    errors: list[str] = []
    schedule: dict[str, list[tuple[datetime, datetime]]] = {}

    for shift in proposed_shifts:
        start = datetime.fromisoformat(shift["start_time"])
        end = datetime.fromisoformat(shift["end_time"])

        for cand in shift.get("assigned_candidates", []):
            vid = str(cand.get("volunteer_id") or cand.get("volunteerId") or "")
            if not vid:
                continue

            intervals = schedule.setdefault(vid, [])
            for (s, e) in intervals:
                if start < e and end > s:
                    name = cand.get("volunteer_name") or vid
                    errors.append(
                        f"Double-booking: {name} is assigned to overlapping shifts."
                    )
            intervals.append((start, end))

    return errors


# ---------------------------------------------------------------------------
# Rule 2: headcount met
# ---------------------------------------------------------------------------
def assert_headcount_met(
    proposed_shifts: list[dict],
    role_requirements: list[dict],
) -> list[str]:
    """
    Checks that each role's headcount requirement is satisfied by the proposal.

    Args:
        proposed_shifts: list of proposed shifts
        role_requirements: list of {id, role_name, required_headcount}

    Returns:
        List of error messages (empty = passes).
    """
    errors: list[str] = []

    # Sum assigned candidates per role
    per_role: dict[str, int] = {}
    for shift in proposed_shifts:
        role = shift.get("role_name") or "Unknown"
        assigned = len(shift.get("assigned_candidates", []))
        per_role[role] = per_role.get(role, 0) + assigned

    # Compare against requirements
    for role in role_requirements:
        name = role.get("roleName") or role.get("role_name") or "Unknown"
        required = int(role.get("requiredHeadcount") or role.get("required_headcount") or 0)
        actual = per_role.get(name, 0)
        if actual < required:
            errors.append(
                f"Understaffed: '{name}' needs {required} volunteer(s), "
                f"but proposal covers {actual}."
            )

    return errors


# ---------------------------------------------------------------------------
# Rule 3: experience match
# ---------------------------------------------------------------------------
def assert_experience_match(
    proposed_shifts: list[dict],
    role_requirements: list[dict],
) -> list[str]:
    """
    Checks every assigned candidate meets the minimum experience tier for
    their role.
    """
    errors: list[str] = []

    min_tier_by_role: dict[str, int] = {}
    for role in role_requirements:
        name = role.get("roleName") or role.get("role_name") or "Unknown"
        min_level = role.get("minExperienceLevel") or role.get("min_experience_level") or "Beginner"
        min_tier_by_role[name] = EXPERIENCE_TIERS.get(min_level.strip().lower(), 1)

    for shift in proposed_shifts:
        role = shift.get("role_name") or "Unknown"
        min_tier = min_tier_by_role.get(role, 1)

        for cand in shift.get("assigned_candidates", []):
            vol_level = str(cand.get("experience_level", "Beginner"))
            vol_tier = EXPERIENCE_TIERS.get(vol_level.strip().lower(), 1)
            if vol_tier < min_tier:
                name = cand.get("volunteer_name", "unknown")
                errors.append(
                    f"Experience mismatch: {name} ({vol_level}) assigned to "
                    f"'{role}' (requires tier >= {min_tier})."
                )

    return errors


# ---------------------------------------------------------------------------
# Rule 4: shift duration
# ---------------------------------------------------------------------------
def assert_shift_duration(
    proposed_shifts: list[dict],
    max_hours: float = 6.0,
) -> list[str]:
    """Checks no shift exceeds the maximum allowed hours."""
    errors: list[str] = []

    for shift in proposed_shifts:
        start = datetime.fromisoformat(shift["start_time"])
        end = datetime.fromisoformat(shift["end_time"])
        hours = (end - start).total_seconds() / 3600
        if hours > max_hours:
            errors.append(
                f"Shift too long: {shift['shift_id'][:8]} is {hours:.1f}h "
                f"(max {max_hours}h)."
            )

    return errors


# ---------------------------------------------------------------------------
# Rule 5: event bounds
# ---------------------------------------------------------------------------
def assert_event_bounds(
    proposed_shifts: list[dict],
    event_start_iso: str,
    event_end_iso: str,
) -> list[str]:
    """Checks all shifts fall inside the event window."""
    errors: list[str] = []

    event_start = datetime.fromisoformat(event_start_iso.replace("Z", "+00:00"))
    event_end = datetime.fromisoformat(event_end_iso.replace("Z", "+00:00"))

    for shift in proposed_shifts:
        start = datetime.fromisoformat(shift["start_time"])
        end = datetime.fromisoformat(shift["end_time"])

        if start < event_start:
            errors.append(
                f"Shift {shift['shift_id'][:8]} starts before the event window."
            )
        if end > event_end:
            errors.append(
                f"Shift {shift['shift_id'][:8]} ends after the event window."
            )

    return errors