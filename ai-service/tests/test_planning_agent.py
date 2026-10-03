"""Tests that the PlanningAgent runs the compiled graph."""

import pytest

from app.agents.planning_agent import PlanningAgent
from app.tools.event_tools import EventSummary
from app.tools.venue_tools import VenueSummary


@pytest.mark.asyncio
async def test_planning_agent_delegates_to_graph_and_returns_tools():
    event = EventSummary(
        id="11111111-1111-1111-1111-111111111111",
        venue_id="22222222-2222-2222-2222-222222222222",
        title="Planning test",
        description=None,
        category="Community",
        start_date="2026-11-01T09:00:00Z",
        end_date="2026-11-01T17:00:00Z",
        status="Draft",
        role_requirements=[],
    )
    venue = VenueSummary(
        id=event.venue_id,
        name="Community Hall",
        address="1 Main Street",
        city="Colombo",
        latitude=None,
        longitude=None,
        capacity=100,
    )

    result = await PlanningAgent().plan(event.id, event, venue)

    assert result.tool_calls
    assert result.tool_calls[-1].tool_name == "calculate_staffing_ratio"
    assert result.staffing_recommendations == []
