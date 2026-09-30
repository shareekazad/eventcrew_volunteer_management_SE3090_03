"""
PlanningAgent — the coordinator.

Responsibility:
    Given an event objective, produce a structured multi-step plan
    for staffing it, then hand off to the next agent.

Input contract:
    event_id: str  — UUID of the event.

Output contract:
    PlanResult — structured plan with steps, reasoning, and tool call log.

Controlled tools:
    This agent can ONLY call: get_event, get_venue, calculate_staffing_ratio.
    Anything else is forbidden.
"""

import time
from datetime import datetime, timezone
from pydantic import BaseModel, Field

from app.tools.event_tools import get_event
from app.tools.venue_tools import get_venue
from app.tools.scheduling_tools import calculate_staffing_ratio


# ---------------------------------------------------------------------------
# Output models (the agent's contract)
# ---------------------------------------------------------------------------
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


class PlanResult(BaseModel):
    objective: str
    event_id: str
    steps: list[PlanStep]
    reasoning: str
    tool_calls: list[ToolCallLog]
    next_agent: str
    status: str = "planned"


# ---------------------------------------------------------------------------
# Agent class
# ---------------------------------------------------------------------------
class PlanningAgent:
    """
    The Planning & Coordinator Agent.

    Uses a fixed, deterministic sequence of tool calls to build a plan.
    Later, an LLM can be inserted to make the sequence adaptive (e.g., for
    unusual event categories) — but the baseline is fully deterministic and testable.
    """

    name = "PlanningAgent"

    # ---- Tool allow-list (Section 9.1 requirement) ----
    ALLOWED_TOOLS = {"get_event", "get_venue", "calculate_staffing_ratio"}

    async def plan(self, event_id: str) -> PlanResult:
        tool_calls: list[ToolCallLog] = []
        steps: list[PlanStep] = []

        # ---- Step 1: Fetch the event ----
        t0 = time.perf_counter()
        event = await get_event(event_id)
        duration_ms = int((time.perf_counter() - t0) * 1000)

        if event is None:
            raise ValueError(f"Event '{event_id}' not found.")

        tool_calls.append(
            ToolCallLog(
                tool_name="get_event",
                input_params={"event_id": event_id},
                output_summary=f"Retrieved event '{event.title}' ({len(event.role_requirements)} roles)",
                duration_ms=duration_ms,
                called_at=datetime.now(timezone.utc).isoformat(),
            )
        )
        steps.append(PlanStep(
            step_number=1,
            action=f"Fetch event details for '{event.title}'",
            tool="get_event",
            agent=self.name,
        ))

        # ---- Step 2: Fetch the venue (if attached) ----
        if event.venue_id is None:
            raise ValueError(f"Event '{event.title}' has no venue assigned; cannot plan staffing.")

        t0 = time.perf_counter()
        venue = await get_venue(event.venue_id)
        duration_ms = int((time.perf_counter() - t0) * 1000)

        if venue is None:
            raise ValueError(f"Venue '{event.venue_id}' not found.")

        tool_calls.append(
            ToolCallLog(
                tool_name="get_venue",
                input_params={"venue_id": event.venue_id},
                output_summary=f"Retrieved venue '{venue.name}' (capacity {venue.capacity})",
                duration_ms=duration_ms,
                called_at=datetime.now(timezone.utc).isoformat(),
            )
        )
        steps.append(PlanStep(
            step_number=2,
            action=f"Fetch venue '{venue.name}' capacity",
            tool="get_venue",
            agent=self.name,
        ))

        # ---- Step 3: Calculate staffing ratio ----
        t0 = time.perf_counter()
        ratio = calculate_staffing_ratio(venue.capacity)
        duration_ms = int((time.perf_counter() - t0) * 1000)

        tool_calls.append(
            ToolCallLog(
                tool_name="calculate_staffing_ratio",
                input_params={"venue_capacity": venue.capacity},
                output_summary=ratio.reasoning,
                duration_ms=duration_ms,
                called_at=datetime.now(timezone.utc).isoformat(),
            )
        )
        steps.append(PlanStep(
            step_number=3,
            action=f"Calculate staffing ratio ({ratio.total_staff} total staff recommended)",
            tool="calculate_staffing_ratio",
            agent=self.name,
        ))

        # ---- Step 4: Handoff to next agent (coordination) ----
        next_agent = "MatchingAgent"
        steps.append(PlanStep(
            step_number=4,
            action=f"Hand off to {next_agent} to rank candidate volunteers",
            tool=None,
            agent=next_agent,
        ))

        # ---- Reasoning (human-readable) ----
        reasoning = (
            f"Event '{event.title}' at '{venue.name}' (capacity {venue.capacity}). "
            f"Based on the staffing rule, {ratio.recommended_ushers} ushers and "
            f"{ratio.recommended_registration_staff} registration staff are recommended "
            f"(total {ratio.total_staff}). Delegating to {next_agent} to select candidates."
        )

        return PlanResult(
            objective=f"Plan staffing for event '{event.title}'",
            event_id=event_id,
            steps=steps,
            reasoning=reasoning,
            tool_calls=tool_calls,
            next_agent=next_agent,
        )