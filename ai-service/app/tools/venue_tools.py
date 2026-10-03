"""Tool that inspects the venue snapshot supplied by the authorized API."""

from pydantic import BaseModel


# ---------------------------------------------------------------------------
# Structured output model
# ---------------------------------------------------------------------------
class VenueSummary(BaseModel):
    id: str
    name: str
    address: str
    city: str
    latitude: float | None
    longitude: float | None
    capacity: int


# ---------------------------------------------------------------------------
# Tools
# ---------------------------------------------------------------------------
async def get_venue(venue_id: str, venue_context: VenueSummary | None) -> VenueSummary | None:
    """
    Inspect the event's authorized venue snapshot.

    Args:
        venue_id: UUID of the venue as a string.

    Returns:
        VenueSummary when its ID matches, else None.
    """
    if venue_context is None:
        return None
    if venue_context.id.casefold() != venue_id.casefold():
        raise ValueError("The supplied venue context does not match the event venue.")
    return venue_context