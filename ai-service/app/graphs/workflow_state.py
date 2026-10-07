"""
WorkflowState — shared state for the multi-agent workflow graph.

Flows through all 4 agent nodes:
    PlanningAgent → MatchingAgent → SchedulingAgent → ValidationAgent

Each node reads what it needs and writes only its own section.
Every node appends to `agent_traces` for the audit trail.
"""

from pydantic import BaseModel, Field

# Reuse existing typed models
from app.tools.event_tools import EventSummary
from app.tools.venue_tools import VenueSummary
from app.tools.scheduling_tools import StaffingRatioResult
from app.schemas.matching_schema import CandidateMatch


class WorkflowState(BaseModel):
    """
    State shared across all 4 agents.
    """

    # ---- Input ----
    event_id: str
    organizer_id: str | None = None
    # Pre-fetched candidates passed from ASP.NET Core to avoid the
    # circular HTTP call (Python → ASP.NET → 401 Unauthorized loop).
    candidates: list[dict] = Field(default_factory=list)

    # ---- Global orchestration ----
    workflow_id: str | None = None
    current_step: int = 0

    # ---- Node 1: PlanningAgent output ----
    event: EventSummary | None = None
    venue: VenueSummary | None = None
    staffing_ratio: StaffingRatioResult | None = None
    plan_steps: list[dict] = Field(default_factory=list)
    plan_reasoning: str | None = None

    # ---- Node 2: MatchingAgent output ----
    matching_results: list[dict] = Field(default_factory=list)
    total_matched: int = 0
    total_headcount_needed: int = 0

    # ---- Node 3: SchedulingAgent output ----
    proposed_shifts: list[dict] = Field(default_factory=list)
    shift_conflicts: list[dict] = Field(default_factory=list)

    # ---- Node 4: ValidationAgent output ----
    validation_passed: bool = False
    validation_errors: list[str] = Field(default_factory=list)
    validation_warnings: list[str] = Field(default_factory=list)

    # ---- Cross-cutting: audit trail ----
    agent_traces: list[dict] = Field(default_factory=list)

    # ---- Lifecycle ----
    status: str = "running"
    error: str | None = None