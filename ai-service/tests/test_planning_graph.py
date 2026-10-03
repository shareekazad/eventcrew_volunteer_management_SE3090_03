"""
Tests for the LangGraph-orchestrated planning workflow.

These tests mock the HTTP-calling tools (get_event, get_venue) so tests
run without a live ASP.NET Core backend.
"""

from unittest.mock import AsyncMock, patch

import pytest

from app.agents.planning_agent import PlanResult
from app.graphs.planning_graph import run_planning_graph
from app.tools.event_tools import EventSummary, RoleRequirementSummary
from app.tools.venue_tools import VenueSummary


# ---------------------------------------------------------------------------
# Fixtures — reusable fake data
# ---------------------------------------------------------------------------
def make_fake_event() -> EventSummary:
    return EventSummary(
        id="11111111-1111-1111-1111-111111111111",
        organizer_id="22222222-2222-2222-2222-222222222222",
        venue_id="33333333-3333-3333-3333-333333333333",
        title="Test Tech Meetup",
        description="A test event",
        category="Conference",
        start_date="2026-11-15T09:00:00+00:00",
        end_date="2026-11-15T18:00:00+00:00",
        status="Draft",
        role_requirements=[
            RoleRequirementSummary(
                id="44444444-4444-4444-4444-444444444444",
                role_name="Usher",
                required_headcount=5,
                min_experience_level="Beginner",
            )
        ],
    )


def make_fake_venue() -> VenueSummary:
    return VenueSummary(
        id="33333333-3333-3333-3333-333333333333",
        name="Test Venue",
        address="123 Test St",
        city="Colombo",
        latitude=6.9,
        longitude=79.86,
        capacity=500,
    )


# ---------------------------------------------------------------------------
# Happy path
# ---------------------------------------------------------------------------
@pytest.mark.asyncio
async def test_run_planning_graph_produces_full_plan():
    """With mocked tools, the graph returns a complete, well-formed plan."""
    with patch(
        "app.graphs.planning_nodes.get_event",
        new=AsyncMock(return_value=make_fake_event()),
    ), patch(
        "app.graphs.planning_nodes.get_venue",
        new=AsyncMock(return_value=make_fake_venue()),
    ):
        result = await run_planning_graph("11111111-1111-1111-1111-111111111111")

    assert isinstance(result, PlanResult)
    assert result.status == "planned"
    assert result.next_agent == "MatchingAgent"
    assert "Test Tech Meetup" in result.objective
    assert len(result.steps) == 4
    assert len(result.tool_calls) == 3

    # Verify the sequence of tool calls
    tool_names = [c.tool_name for c in result.tool_calls]
    assert tool_names == ["get_event", "get_venue", "calculate_staffing_ratio"]


@pytest.mark.asyncio
async def test_run_planning_graph_includes_reasoning():
    """Reasoning string mentions event, venue and totals."""
    with patch(
        "app.graphs.planning_nodes.get_event",
        new=AsyncMock(return_value=make_fake_event()),
    ), patch(
        "app.graphs.planning_nodes.get_venue",
        new=AsyncMock(return_value=make_fake_venue()),
    ):
        result = await run_planning_graph("11111111-1111-1111-1111-111111111111")

    assert "Test Tech Meetup" in result.reasoning
    assert "Test Venue" in result.reasoning
    assert "capacity 500" in result.reasoning


# ---------------------------------------------------------------------------
# Failure paths
# ---------------------------------------------------------------------------
@pytest.mark.asyncio
async def test_run_planning_graph_raises_when_event_not_found():
    """Event missing → ValueError with a clear message."""
    with patch(
        "app.graphs.planning_nodes.get_event",
        new=AsyncMock(return_value=None),
    ):
        with pytest.raises(ValueError, match="not found"):
            await run_planning_graph("00000000-0000-0000-0000-000000000000")


@pytest.mark.asyncio
async def test_run_planning_graph_raises_when_event_has_no_venue():
    """Event without venue → ValueError explaining why."""
    fake_event = make_fake_event()
    fake_event.venue_id = None

    with patch(
        "app.graphs.planning_nodes.get_event",
        new=AsyncMock(return_value=fake_event),
    ):
        with pytest.raises(ValueError, match="no venue"):
            await run_planning_graph("11111111-1111-1111-1111-111111111111")


@pytest.mark.asyncio
async def test_run_planning_graph_raises_when_venue_not_found():
    """Venue ID exists on event but missing in backend → ValueError."""
    with patch(
        "app.graphs.planning_nodes.get_event",
        new=AsyncMock(return_value=make_fake_event()),
    ), patch(
        "app.graphs.planning_nodes.get_venue",
        new=AsyncMock(return_value=None),
    ):
        with pytest.raises(ValueError, match="Venue"):
            await run_planning_graph("11111111-1111-1111-1111-111111111111")