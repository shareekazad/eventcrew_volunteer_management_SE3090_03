"""
Tools for interacting with events via the ASP.NET Core backend.

Each tool is a small, focused async function with a clear contract:
- takes validated input
- calls the backend
- returns a structured result (Pydantic model) or None
"""

from pydantic import BaseModel
from app.tools.http_client import BackendClient


# ---------------------------------------------------------------------------
# Structured output models
# ---------------------------------------------------------------------------
class RoleRequirementSummary(BaseModel):
    id: str
    role_name: str
    required_headcount: int
    min_experience_level: str


class EventSummary(BaseModel):
    id: str
    organizer_id: str
    venue_id: str | None
    title: str
    description: str | None
    category: str
    start_date: str
    end_date: str
    status: str
    role_requirements: list[RoleRequirementSummary]


# ---------------------------------------------------------------------------
# Tools
# ---------------------------------------------------------------------------
async def get_event(event_id: str) -> EventSummary | None:
    """
    Fetch an event (with nested role requirements) from the backend.

    Args:
        event_id: UUID of the event as a string.

    Returns:
        EventSummary if found, else None.
    """
    async with BackendClient() as client:
        data = await client.get(f"/api/Events/{event_id}")

    if data is None:
        return None

    # Map nested role requirements into a flat, typed shape
    roles = [
        RoleRequirementSummary(
            id=r["id"],
            role_name=r["roleName"],
            required_headcount=r["requiredHeadcount"],
            min_experience_level=r["minExperienceLevel"],
        )
        for r in data.get("roleRequirements", [])
    ]

    return EventSummary(
        id=data["id"],
        organizer_id=data["organizerId"],
        venue_id=data.get("venueId"),
        title=data["title"],
        description=data.get("description"),
        category=data["category"],
        start_date=data["startDate"],
        end_date=data["endDate"],
        status=data["status"],
        role_requirements=roles,
    )