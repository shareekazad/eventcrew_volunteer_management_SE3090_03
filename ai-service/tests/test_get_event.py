"""Unit tests for the event-context tool (no backend credentials or network)."""
import pytest
from app.tools.event_tools import get_event
from app.tools.event_tools import EventSummary


@pytest.mark.asyncio
async def test_get_event_returns_only_matching_authorized_snapshot():
    event = EventSummary(
        id="11111111-1111-1111-1111-111111111111",
        venue_id=None,
        title="Authorized event",
        description=None,
        category="Community",
        start_date="2026-11-01T09:00:00Z",
        end_date="2026-11-01T17:00:00Z",
        status="Draft",
        role_requirements=[],
    )

    assert await get_event(event.id, event) is event
    with pytest.raises(ValueError, match="does not match"):
        await get_event("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", event)