"""
PlanningState — the shared state that flows through the planning graph.

Each node in the graph receives this state, does its work, and returns
a partial update (a dict with only the fields it changed).

The final state is turned into a PlanResult at the end of the graph.
"""

from pydantic import BaseModel, Field

from app.tools.event_tools import EventSummary
from app.tools.venue_tools import VenueSummary
from app.tools.scheduling_tools import StaffingRatioResult


class PlanningState(BaseModel):
    """
    State for the planning workflow.

    Fields marked as `| None` start as None and get populated by nodes
    as the graph progresses.
    """

    # ---- Input ----
    event_id: str

    # ---- Intermediate results (filled by nodes) ----
    event: EventSummary | None = None
    venue: VenueSummary | None = None
    ratio: StaffingRatioResult | None = None

    # ---- Output (assembled at the end) ----
    objective: str | None = None
    reasoning: str | None = None
    steps: list[dict] = Field(default_factory=list)
    tool_calls: list[dict] = Field(default_factory=list)
    next_agent: str | None = None
    status: str = "running"

    # ---- Error tracking ----
    error: str | None = None