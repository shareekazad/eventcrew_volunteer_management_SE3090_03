"""
Deterministic tools for staffing calculations and volunteer scheduling.

No external LLM is used here. The scheduling logic is simple, deterministic,
and constraint-driven.
"""

import math
from collections import defaultdict
from datetime import datetime, timezone
from typing import Any

from pydantic import BaseModel, Field


class StaffingRatioResult(BaseModel):
    venue_capacity: int
    recommended_ushers: int
    recommended_registration_staff: int
    total_staff: int
    reasoning: str


class VolunteerSummary(BaseModel):
    id: str
    full_name: str = ""
    email: str | None = None
    is_active: bool = True
    max_hours: int | None = None
    role_preferences: list[str] = Field(default_factory=list)
    eligible_roles: list[str] = Field(default_factory=list)
    availability: list[dict[str, Any]] = Field(default_factory=list)


class ShiftSummary(BaseModel):
    id: str
    event_id: str | None = None
    role_requirement_id: str | None = None
    title: str = ""
    role_name: str | None = None
    start_time: str | None = None
    end_time: str | None = None
    capacity: int = 1
    status: str = "Scheduled"


class RosterEntry(BaseModel):
    volunteer_id: str
    volunteer_name: str
    role: str
    shift_id: str
    shift_start: str
    shift_end: str
    hours: int


class UnfilledSlot(BaseModel):
    shift_id: str
    role: str
    shift_start: str
    shift_end: str
    reason: str


class ScheduleValidation(BaseModel):
    availability_violations: int = 0
    overlap_violations: int = 0
    hour_limit_violations: int = 0
    role_requirement_violations: int = 0
    total_assigned_volunteers: int = 0
    total_unfilled_slots: int = 0
    is_valid: bool = True


def calculate_staffing_ratio(venue_capacity: int) -> StaffingRatioResult:
    """Calculate recommended staffing based on venue capacity."""
    if venue_capacity <= 0:
        raise ValueError("Venue capacity must be greater than 0.")

    ushers = max(2, math.ceil(venue_capacity / 100))
    registration = max(1, math.ceil(venue_capacity / 300))

    reasoning = (
        f"Capacity {venue_capacity}: "
        f"{ushers} ushers (1 per 100 attendees, min 2), "
        f"{registration} registration staff (1 per 300 attendees, min 1)."
    )

    return StaffingRatioResult(
        venue_capacity=venue_capacity,
        recommended_ushers=ushers,
        recommended_registration_staff=registration,
        total_staff=ushers + registration,
        reasoning=reasoning,
    )


def _coerce_datetime(value: str | None) -> datetime | None:
    if value is None:
        return None
    if isinstance(value, datetime):
        return value
    text = value.strip()
    if not text:
        return None
    candidate = text.replace("Z", "+00:00")
    try:
        return datetime.fromisoformat(candidate)
    except ValueError:
        return None


def _parse_availability_window(item: dict[str, Any]) -> tuple[datetime | None, datetime | None] | None:
    start = item.get("start") or item.get("start_time") or item.get("from")
    end = item.get("end") or item.get("end_time") or item.get("to")
    if start is None or end is None:
        return None

    start_dt = _coerce_datetime(str(start))
    end_dt = _coerce_datetime(str(end))
    if start_dt is None or end_dt is None:
        return None
    return start_dt.astimezone(timezone.utc), end_dt.astimezone(timezone.utc)


def _overlaps(start_a: datetime, end_a: datetime, start_b: datetime, end_b: datetime) -> bool:
    return max(start_a, start_b) < min(end_a, end_b)


def _normalize_shift(shift: dict[str, Any]) -> dict[str, Any]:
    start_time = shift.get("start_time") or shift.get("start")
    end_time = shift.get("end_time") or shift.get("end")
    role_name = shift.get("role_name") or shift.get("role") or shift.get("roleRequirementName")
    title = shift.get("title") or role_name or "Shift"
    capacity = int(shift.get("capacity") or 1)
    return {
        "id": str(shift.get("id") or "shift-unknown"),
        "event_id": str(shift.get("event_id") or ""),
        "role_requirement_id": str(shift.get("role_requirement_id") or shift.get("roleRequirementId") or ""),
        "title": str(title),
        "role_name": str(role_name or "General"),
        "start_time": str(start_time or ""),
        "end_time": str(end_time or ""),
        "capacity": max(1, capacity),
        "status": str(shift.get("status") or "Scheduled"),
    }


def _normalize_volunteer(volunteer: dict[str, Any]) -> dict[str, Any]:
    role_preferences = volunteer.get("role_preferences") or volunteer.get("preferred_roles") or []
    eligible_roles = volunteer.get("eligible_roles") or volunteer.get("qualified_roles") or volunteer.get("skills") or []
    availability = volunteer.get("availability") or []
    return {
        "id": str(volunteer.get("id") or volunteer.get("volunteer_id") or ""),
        "full_name": str(volunteer.get("full_name") or volunteer.get("name") or volunteer.get("fullName") or "Volunteer"),
        "email": volunteer.get("email"),
        "is_active": bool(volunteer.get("is_active", True)),
        "max_hours": volunteer.get("max_hours"),
        "role_preferences": [str(item) for item in role_preferences],
        "eligible_roles": [str(item) for item in eligible_roles],
        "availability": [dict(item) for item in availability],
    }


def _role_matches(volunteer: dict[str, Any], shift_role: str) -> bool:
    if not shift_role:
        return True
    preferences = {str(item).casefold() for item in volunteer.get("role_preferences", [])}
    eligible = {str(item).casefold() for item in volunteer.get("eligible_roles", [])}
    if preferences or eligible:
        return (
            shift_role.casefold() in preferences
            or shift_role.casefold() in eligible
            or any(token.casefold() in preferences or token.casefold() in eligible for token in [shift_role])
        )
    return True


def _availability_matches(volunteer: dict[str, Any], shift_start: datetime, shift_end: datetime) -> bool:
    availability = volunteer.get("availability") or []
    if not availability:
        return True
    for window in availability:
        parsed = _parse_availability_window(window)
        if parsed is None:
            continue
        available_start, available_end = parsed
        if available_start is None or available_end is None:
            continue
        if _overlaps(shift_start, shift_end, available_start, available_end):
            return True
    return False


def build_roster(
    event: Any,
    shifts: list[dict[str, Any]] | None = None,
    volunteers: list[dict[str, Any]] | None = None,
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], dict[str, Any]]:
    """Construct a valid volunteer roster for an event when shift and volunteer data exist."""
    normalized_shifts = [_normalize_shift(s) for s in (shifts or [])]
    normalized_volunteers = [_normalize_volunteer(v) for v in (volunteers or [])]

    if not normalized_shifts or not normalized_volunteers:
        return [], [], {
            "availability_violations": 0,
            "overlap_violations": 0,
            "hour_limit_violations": 0,
            "role_requirement_violations": 0,
            "total_assigned_volunteers": 0,
            "total_unfilled_slots": 0,
            "is_valid": True,
        }

    by_volunteer_hours: dict[str, int] = defaultdict(int)
    roster: list[dict[str, Any]] = []
    unfilled_slots: list[dict[str, Any]] = []
    assigned_windows: dict[str, list[tuple[datetime, datetime]]] = defaultdict(list)

    for shift in sorted(normalized_shifts, key=lambda item: (item["start_time"], item["role_name"])):
        shift_start = _coerce_datetime(shift.get("start_time"))
        shift_end = _coerce_datetime(shift.get("end_time"))
        if shift_start is None or shift_end is None:
            unfilled_slots.append({
                "shift_id": shift["id"],
                "role": shift["role_name"],
                "shift_start": shift["start_time"],
                "shift_end": shift["end_time"],
                "reason": "Shift has no valid start or end time.",
            })
            continue

        candidates: list[tuple[int, int, str, dict[str, Any]]] = []
        for volunteer in normalized_volunteers:
            if not volunteer.get("is_active", True):
                continue
            volunteer_id = volunteer["id"]
            if any(_overlaps(shift_start, shift_end, start_dt, end_dt) for start_dt, end_dt in assigned_windows.get(volunteer_id, [])):
                continue
            if not _availability_matches(volunteer, shift_start, shift_end):
                continue
            if not _role_matches(volunteer, shift["role_name"]):
                continue
            max_hours = volunteer.get("max_hours")
            assigned_hours = by_volunteer_hours.get(volunteer_id, 0)
            shift_duration_hours = max(1, int(math.ceil((shift_end - shift_start).total_seconds() / 3600)))
            if isinstance(max_hours, (int, float)) and assigned_hours + shift_duration_hours > int(max_hours):
                continue
            preference_penalty = 0
            if volunteer.get("role_preferences") and shift["role_name"].casefold() not in {item.casefold() for item in volunteer["role_preferences"]}:
                preference_penalty = 1
            candidates.append((preference_penalty, assigned_hours, volunteer["full_name"], volunteer))

        if not candidates:
            unfilled_slots.append({
                "shift_id": shift["id"],
                "role": shift["role_name"],
                "shift_start": shift["start_time"],
                "shift_end": shift["end_time"],
                "reason": "No eligible volunteer was available within the required time window.",
            })
            continue

        chosen = sorted(candidates, key=lambda item: (item[0], item[1], item[2]))[0][3]
        volunteer_id = chosen["id"]
        shift_duration_hours = max(1, int(math.ceil((shift_end - shift_start).total_seconds() / 3600)))
        by_volunteer_hours[volunteer_id] += shift_duration_hours
        assigned_windows[volunteer_id].append((shift_start, shift_end))
        roster.append({
            "volunteer_id": volunteer_id,
            "volunteer_name": chosen["full_name"],
            "role": shift["role_name"],
            "shift_id": shift["id"],
            "shift_start": shift["start_time"],
            "shift_end": shift["end_time"],
            "hours": shift_duration_hours,
        })

    availability_violations = 0
    overlap_violations = 0
    hour_limit_violations = 0
    role_requirement_violations = 0

    volunteers_by_id = {str(v["id"]): v for v in normalized_volunteers}
    for assignment in roster:
        volunteer = volunteers_by_id.get(assignment["volunteer_id"])
        if volunteer is None:
            continue
        start_dt = _coerce_datetime(assignment["shift_start"])
        end_dt = _coerce_datetime(assignment["shift_end"])
        if start_dt is None or end_dt is None:
            availability_violations += 1
            continue
        if not _availability_matches(volunteer, start_dt, end_dt):
            availability_violations += 1
        if not _role_matches(volunteer, assignment["role"]):
            role_requirement_violations += 1

    volunteer_windows: dict[str, list[tuple[datetime, datetime]]] = defaultdict(list)
    for assignment in roster:
        start_dt = _coerce_datetime(assignment["shift_start"])
        end_dt = _coerce_datetime(assignment["shift_end"])
        if start_dt is not None and end_dt is not None:
            volunteer_windows[assignment["volunteer_id"]].append((start_dt, end_dt))

    for windows in volunteer_windows.values():
        for index, (start_a, end_a) in enumerate(windows):
            for start_b, end_b in windows[index + 1:]:
                if _overlaps(start_a, end_a, start_b, end_b):
                    overlap_violations += 1

    for volunteer in normalized_volunteers:
        volunteer_id = str(volunteer["id"])
        max_hours = volunteer.get("max_hours")
        if isinstance(max_hours, (int, float)) and by_volunteer_hours.get(volunteer_id, 0) > int(max_hours):
            hour_limit_violations += 1

    validation = {
        "availability_violations": availability_violations,
        "overlap_violations": overlap_violations,
        "hour_limit_violations": hour_limit_violations,
        "role_requirement_violations": role_requirement_violations,
        "total_assigned_volunteers": len({entry["volunteer_id"] for entry in roster}),
        "total_unfilled_slots": len(unfilled_slots),
        "is_valid": (
            availability_violations == 0
            and overlap_violations == 0
            and hour_limit_violations == 0
            and role_requirement_violations == 0
            and len(unfilled_slots) == 0
        ),
    }

    return roster, unfilled_slots, validation

