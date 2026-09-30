"""
LangGraph orchestration for the planning workflow.

Wires the 4 node functions from planning_nodes.py into a state graph:

    START → fetch_event → [check] → fetch_venue → [check]
                       → calculate_ratio → [check] → build_plan → END

Each [check] is a conditional edge that short-circuits to END if a node
reported an error. This ensures safe failure: the graph stops on the first
error instead of overwriting it with a generic downstream message.
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
# Router — decides whether to continue or stop after each node
# ---------------------------------------------------------------------------
def _route_after_node(next_node: str):
    """
    Returns a routing function that:
    - returns 'stop' if the state has an error
    - returns next_node otherwise
    """
    def router(state: PlanningState) -> str:
        if state.status == "failed" or state.error:
            return "stop"
        return next_node
    return router


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
# 3. Wire edges with conditional checks after each node
# ---------------------------------------------------------------------------
_builder.add_edge(START, "fetch_event")

_builder.add_conditional_edges(
    "fetch_event",
    _route_after_node("fetch_venue"),
    {"fetch_venue": "fetch_venue", "stop": END},
)

_builder.add_conditional_edges(
    "fetch_venue",
    _route_after_node("calculate_ratio"),
    {"calculate_ratio": "calculate_ratio", "stop": END},
)

_builder.add_conditional_edges(
    "calculate_ratio",
    _route_after_node("build_plan"),
    {"build_plan": "build_plan", "stop": END},
)

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

    Raises ValueError if the graph failed at any node, with the ORIGINAL
    error message from that node (not a generic downstream message).
    """
    initial_state = PlanningState(event_id=event_id)

    final_state = await planning_graph.ainvoke(initial_state)

    if isinstance(final_state, dict):
        final_state = PlanningState(**final_state)

    if final_state.status == "failed" or final_state.error:
        raise ValueError(final_state.error or "Planning graph failed.")

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