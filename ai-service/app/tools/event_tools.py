"""Tools that inspect the event snapshot supplied by the authorized API."""

from pydantic import BaseModel


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
async def get_event(event_id: str, event_context: EventSummary | None) -> EventSummary | None:
    """
    Inspect the event snapshot authorized and supplied by ASP.NET Core.

    Args:
        event_id: UUID of the event as a string.

    Returns:
        EventSummary when its ID matches, else None.
    """
    if event_context is None:
        return None
    if event_context.id.casefold() != event_id.casefold():
        raise ValueError("The supplied event context does not match the requested event.")
    return event_context