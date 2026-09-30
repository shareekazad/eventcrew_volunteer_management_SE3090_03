"""
LangGraph orchestration for the planning workflow.

Wires the 4 node functions from planning_nodes.py into a state graph:

    START → fetch_event → fetch_venue → calculate_ratio → build_plan → END

Exposes run_planning_graph(event_id) as the high-level entry point.
"""

from langgraph.graph import StateGraph, START, END

from app.graphs.planning_state import PlanningState
from app.graphs.planning_nodes import (
    fetch_event_node,
    fetch_venue_node,
    calculate_ratio_node,
    build_plan_node,
)
from app.agents.planning_agent import PlanResult, PlanStep, ToolCallLog


# ---------------------------------------------------------------------------
# 1. Create the graph
# ---------------------------------------------------------------------------
_builder = StateGraph(PlanningState)

# ---------------------------------------------------------------------------
# 2. Register nodes
# ---------------------------------------------------------------------------
_builder.add_node("fetch_event", fetch_event_node)
_builder.add_node("fetch_venue", fetch_venue_node)
_builder.add_node("calculate_ratio", calculate_ratio_node)
_builder.add_node("build_plan", build_plan_node)

# ---------------------------------------------------------------------------
# 3. Wire the edges (linear flow for now)
# ---------------------------------------------------------------------------
_builder.add_edge(START, "fetch_event")
_builder.add_edge("fetch_event", "fetch_venue")
_builder.add_edge("fetch_venue", "calculate_ratio")
_builder.add_edge("calculate_ratio", "build_plan")
_builder.add_edge("build_plan", END)

# ---------------------------------------------------------------------------
# 4. Compile into a runnable graph
# ---------------------------------------------------------------------------
planning_graph = _builder.compile()


# ---------------------------------------------------------------------------
# 5. High-level entry point
# ---------------------------------------------------------------------------
async def run_planning_graph(event_id: str) -> PlanResult:
    """
    Run the LangGraph-orchestrated planning workflow.

    Returns a PlanResult with the same shape as the previous
    PlanningAgent output, so the FastAPI endpoint and ASP.NET Core
    integration stay unchanged.
    """
    initial_state = PlanningState(event_id=event_id)

    final_state = await planning_graph.ainvoke(initial_state)

    # LangGraph returns a dict — re-hydrate to PlanningState for type safety
    if isinstance(final_state, dict):
        final_state = PlanningState(**final_state)

    if final_state.status == "failed" or final_state.error:
        raise ValueError(final_state.error or "Planning graph failed.")

    # ---- Translate internal state into the public PlanResult ----
    steps = [PlanStep(**s) for s in final_state.steps]
    tool_calls = [ToolCallLog(**c) for c in final_state.tool_calls]

    return PlanResult(
        objective=final_state.objective or "",
        event_id=final_state.event_id,
        steps=steps,
        reasoning=final_state.reasoning or "",
        tool_calls=tool_calls,
        next_agent=final_state.next_agent or "Unknown",
        status=final_state.status,
    )