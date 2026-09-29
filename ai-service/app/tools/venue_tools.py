"""
Tools for interacting with venues via the ASP.NET Core backend.
"""

from pydantic import BaseModel
from app.tools.http_client import BackendClient


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
async def get_venue(venue_id: str) -> VenueSummary | None:
    """
    Fetch a venue by ID from the backend.

    Args:
        venue_id: UUID of the venue as a string.

    Returns:
        VenueSummary if found, else None.
    """
    async with BackendClient() as client:
        data = await client.get(f"/api/Venues/{venue_id}")

    if data is None:
        return None

    return VenueSummary(
        id=data["id"],
        name=data["name"],
        address=data["address"],
        city=data["city"],
        latitude=data.get("latitude"),
        longitude=data.get("longitude"),
        capacity=data["capacity"],
    )