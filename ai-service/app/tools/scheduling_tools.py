"""
Deterministic tools for staffing calculations.

No AI here — just math based on business rules. The agent decides WHEN to call
these tools; the tools themselves are predictable and testable.
"""

import math
from pydantic import BaseModel


class StaffingRatioResult(BaseModel):
    venue_capacity: int
    recommended_ushers: int
    recommended_registration_staff: int
    total_staff: int
    reasoning: str


def calculate_staffing_ratio(venue_capacity: int) -> StaffingRatioResult:
    """
    Calculate recommended staffing based on venue capacity.

    Rules:
    - 1 usher per 100 attendees (minimum 2)
    - 1 registration staff per 300 attendees (minimum 1)

    Args:
        venue_capacity: Maximum number of attendees.

    Returns:
        StaffingRatioResult with counts and reasoning.
    """
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