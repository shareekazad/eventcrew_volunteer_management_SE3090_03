"""Coordinator and structured result models for the planning graph."""

from pydantic import BaseModel, Field

from app.tools.event_tools import EventSummary
from app.tools.venue_tools import VenueSummary


class PlanStep(BaseModel):
    step_number: int
    action: str
    tool: str | None
    agent: str
    status: str = "planned"


class ToolCallLog(BaseModel):
    tool_name: str
    input_params: dict
    output_summary: str
    duration_ms: int
    called_at: str


class RoleStaffingRecommendation(BaseModel):
    role_name: str
    required_headcount: int
    minimum_experience_level: str


class PlanResult(BaseModel):
    objective: str
    event_id: str
    steps: list[PlanStep]
    reasoning: str
    tool_calls: list[ToolCallLog]
    staffing_recommendations: list[RoleStaffingRecommendation] = Field(default_factory=list)
    roster: list[dict] = Field(default_factory=list)
    unfilled_slots: list[dict] = Field(default_factory=list)
    validation: dict | None = None
    next_agent: str
    status: str = "planned"


class PlanningAgent:
    """Coordinates the allow-listed tools through the compiled LangGraph."""

    name = "PlanningAgent"
    ALLOWED_TOOLS = {"get_event", "get_venue", "calculate_staffing_ratio"}

    async def plan(
        self,
        event_id: str,
        event: EventSummary,
        venue: VenueSummary | None,
        shifts: list[dict] | None = None,
        volunteers: list[dict] | None = None,
    ) -> PlanResult:
        # Import here to avoid a cycle: the graph uses PlanResult's models.
        from app.graphs.planning_graph import run_planning_graph

        return await run_planning_graph(event_id, event, venue, shifts or [], volunteers or [])
