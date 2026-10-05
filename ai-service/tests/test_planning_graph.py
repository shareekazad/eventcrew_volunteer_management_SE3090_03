"""Tests for the real LangGraph plan flow using authorized input snapshots."""

import pytest

from app.agents.planning_agent import PlanResult
from app.graphs.planning_graph import run_planning_graph
from app.tools.event_tools import EventSummary, RoleRequirementSummary
from app.tools.venue_tools import VenueSummary


EVENT_ID = "11111111-1111-1111-1111-111111111111"
VENUE_ID = "33333333-3333-3333-3333-333333333333"


def make_event() -> EventSummary:
    return EventSummary(
        id=EVENT_ID,
        venue_id=VENUE_ID,
        title="Test Tech Meetup",
        description="A test event",
        category="Conference",
        start_date="2026-11-15T09:00:00+00:00",
        end_date="2026-11-15T18:00:00+00:00",
        status="Draft",
        role_requirements=[
            RoleRequirementSummary(
                id="44444444-4444-4444-4444-444444444444",
                role_name="Guide",
                required_headcount=5,
                min_experience_level="Beginner",
            )
        ],
    )


def make_venue() -> VenueSummary:
    return VenueSummary(
        id=VENUE_ID,
        name="Test Venue",
        address="123 Test St",
        city="Colombo",
        latitude=6.9,
        longitude=79.86,
        capacity=500,
    )


@pytest.mark.asyncio
async def test_graph_runs_allowlisted_tools_and_returns_structured_staffing_plan():
    result = await run_planning_graph(EVENT_ID, make_event(), make_venue())

    assert isinstance(result, PlanResult)
    assert result.status == "planned"
    assert result.next_agent == "OrganizerReview"
    assert "Test Tech Meetup" in result.objective
    assert len(result.steps) >= 4
    assert [call.tool_name for call in result.tool_calls if call.tool_name in {"get_event", "get_venue", "calculate_staffing_ratio"}] == [
        "get_event",
        "get_venue",
        "calculate_staffing_ratio",
    ]
    assert result.staffing_recommendations[0].model_dump() == {
        "role_name": "Guide",
        "required_headcount": 5,
        "minimum_experience_level": "Beginner",
    }
    assert "capacity 500" in result.reasoning
    assert "does not assign individual volunteers" in result.reasoning


@pytest.mark.asyncio
async def test_graph_stops_when_event_has_no_venue():
    event = make_event()
    event.venue_id = None

    with pytest.raises(ValueError, match="no venue"):
        await run_planning_graph(EVENT_ID, event, None)


@pytest.mark.asyncio
async def test_graph_rejects_mismatched_event_snapshot():
    event = make_event()
    event.id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"

    with pytest.raises(ValueError, match="does not match"):
        await run_planning_graph(EVENT_ID, event, make_venue())


@pytest.mark.asyncio
async def test_graph_rejects_mismatched_venue_snapshot():
    venue = make_venue()
    venue.id = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

    with pytest.raises(ValueError, match="does not match"):
        await run_planning_graph(EVENT_ID, make_event(), venue)
