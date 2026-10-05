"""
Unified multi-agent workflow — the LangGraph StateGraph.

Wires the 4 agent nodes into one graph:

    START → plan → [check] → match → [check] → schedule → [check]
          → validate → END

Each [check] is a conditional edge that short-circuits to END
if the state reported an error — this is the safe-failure pattern.
"""

import logging
from langgraph.graph import StateGraph, START, END

from app.graphs.workflow_state import WorkflowState
from app.graphs.workflow_nodes import (
    plan_node,
    match_node,
    schedule_node,
    validate_node,
)


logger = logging.getLogger("eventcrew-ai.workflow_graph")


# ---------------------------------------------------------------------------
# Router — decides whether to continue or stop after each node
# ---------------------------------------------------------------------------
def _route_after_node(next_node: str):
    """
    Returns a router function that:
    - returns 'stop' if the state reported a failure
    - returns next_node otherwise
    """
    def router(state: WorkflowState) -> str:
        if state.status == "failed" or state.error:
            return "stop"
        return next_node
    return router


# ---------------------------------------------------------------------------
# 1. Create the graph
# ---------------------------------------------------------------------------
_builder = StateGraph(WorkflowState)

# ---------------------------------------------------------------------------
# 2. Register the 4 nodes
# ---------------------------------------------------------------------------
_builder.add_node("plan", plan_node)
_builder.add_node("match", match_node)
_builder.add_node("schedule", schedule_node)
_builder.add_node("validate", validate_node)

# ---------------------------------------------------------------------------
# 3. Wire edges with conditional checks
# ---------------------------------------------------------------------------
_builder.add_edge(START, "plan")

_builder.add_conditional_edges(
    "plan",
    _route_after_node("match"),
    {"match": "match", "stop": END},
)

_builder.add_conditional_edges(
    "match",
    _route_after_node("schedule"),
    {"schedule": "schedule", "stop": END},
)

_builder.add_conditional_edges(
    "schedule",
    _route_after_node("validate"),
    {"validate": "validate", "stop": END},
)

_builder.add_edge("validate", END)

# ---------------------------------------------------------------------------
# 4. Compile
# ---------------------------------------------------------------------------
workflow_graph = _builder.compile()


# ---------------------------------------------------------------------------
# 5. High-level entry point
# ---------------------------------------------------------------------------
async def run_workflow(event_id: str) -> dict:
    """
    Run the full 4-agent workflow for an event.

    Returns a dict containing the full WorkflowState:
    - event, venue, staffing_ratio (planning)
    - matching_results, total_matched (matching)
    - proposed_shifts, shift_conflicts (scheduling)
    - validation_passed, validation_errors, validation_warnings (validation)
    - agent_traces (audit trail)
    - status, error

    Raises ValueError if the workflow failed at any node.
    """
    initial_state = WorkflowState(event_id=event_id)

    final_state = await workflow_graph.ainvoke(initial_state)

    # Normalize: LangGraph sometimes returns dict, sometimes Pydantic
    if isinstance(final_state, dict):
        final_state = WorkflowState(**final_state)

    if final_state.status == "failed" or final_state.error:
        raise ValueError(final_state.error or "Workflow failed.")

    return final_state.model_dump(mode="json")