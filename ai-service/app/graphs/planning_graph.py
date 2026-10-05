"""LangGraph orchestration for the scheduling workflow."""

from langgraph.graph import StateGraph, START, END

from app.agents.planning_agent import (
    PlanResult,
    PlanStep,
    RoleStaffingRecommendation,
    ToolCallLog,
)
from app.graphs.planning_state import PlanningState
from app.graphs.planning_nodes import (
    fetch_event_node,
    fetch_venue_node,
    calculate_ratio_node,
    load_shifts_node,
    load_volunteers_node,
    build_schedule_node,
    validate_schedule_node,
    generate_roster_node,
    build_plan_node,
)
from app.tools.event_tools import EventSummary
from app.tools.venue_tools import VenueSummary


def _route_after_node(next_node: str):
    def router(state: PlanningState) -> str:
        if state.status == "failed" or state.error:
            return "stop"
        return next_node
    return router


_builder = StateGraph(PlanningState)
_builder.add_node("fetch_event", fetch_event_node)
_builder.add_node("fetch_venue", fetch_venue_node)
_builder.add_node("calculate_ratio", calculate_ratio_node)
_builder.add_node("load_shifts", load_shifts_node)
_builder.add_node("load_volunteers", load_volunteers_node)
_builder.add_node("build_schedule", build_schedule_node)
_builder.add_node("validate_schedule", validate_schedule_node)
_builder.add_node("generate_roster", generate_roster_node)
_builder.add_node("build_plan", build_plan_node)

_builder.add_edge(START, "fetch_event")
_builder.add_conditional_edges("fetch_event", _route_after_node("fetch_venue"), {"fetch_venue": "fetch_venue", "stop": END})
_builder.add_conditional_edges("fetch_venue", _route_after_node("calculate_ratio"), {"calculate_ratio": "calculate_ratio", "stop": END})
_builder.add_conditional_edges("calculate_ratio", _route_after_node("load_shifts"), {"load_shifts": "load_shifts", "stop": END})
_builder.add_conditional_edges("load_shifts", _route_after_node("load_volunteers"), {"load_volunteers": "load_volunteers", "stop": END})
_builder.add_conditional_edges("load_volunteers", _route_after_node("build_schedule"), {"build_schedule": "build_schedule", "stop": END})
_builder.add_conditional_edges("build_schedule", _route_after_node("validate_schedule"), {"validate_schedule": "validate_schedule", "stop": END})
_builder.add_conditional_edges("validate_schedule", _route_after_node("generate_roster"), {"generate_roster": "generate_roster", "stop": END})
_builder.add_conditional_edges("generate_roster", _route_after_node("build_plan"), {"build_plan": "build_plan", "stop": END})
_builder.add_edge("build_plan", END)

planning_graph = _builder.compile()


async def run_planning_graph(
    event_id: str,
    event_context: EventSummary,
    venue_context: VenueSummary | None,
    shifts: list[dict] | None = None,
    volunteers: list[dict] | None = None,
) -> PlanResult:
    if not shifts and not volunteers:
        state = PlanningState(
            event_id=event_id,
            event_context=event_context,
            venue_context=venue_context,
        )
        event_update = await fetch_event_node(state)
        state = state.model_copy(update=event_update)
        if state.status == "failed" or state.error:
            raise ValueError(state.error or "Planning graph failed.")

        venue_update = await fetch_venue_node(state)
        state = state.model_copy(update=venue_update)
        if state.status == "failed" or state.error:
            raise ValueError(state.error or "Planning graph failed.")

        ratio_update = await calculate_ratio_node(state)
        state = state.model_copy(update=ratio_update)

        plan_update = await build_plan_node(state)
        state = state.model_copy(update=plan_update)
        final_state = state
    else:
        initial_state = PlanningState(
            event_id=event_id,
            event_context=event_context,
            venue_context=venue_context,
            shifts=shifts or [],
            volunteers=volunteers or [],
        )

        final_state = await planning_graph.ainvoke(initial_state)

    if isinstance(final_state, dict):
        final_state = PlanningState(**final_state)

    if final_state.status == "failed" or final_state.error:
        raise ValueError(final_state.error or "Planning graph failed.")

    steps = [PlanStep(**s) for s in final_state.steps]
    tool_calls = [ToolCallLog(**c) for c in final_state.tool_calls]
    staffing_recommendations = [
        RoleStaffingRecommendation(**recommendation)
        for recommendation in final_state.staffing_recommendations
    ]

    return PlanResult(
        objective=final_state.objective or "",
        event_id=final_state.event_id,
        steps=steps,
        reasoning=final_state.reasoning or "",
        tool_calls=tool_calls,
        staffing_recommendations=staffing_recommendations,
        roster=final_state.roster,
        unfilled_slots=final_state.unfilled_slots,
        validation=final_state.validation,
        next_agent=final_state.next_agent or "OrganizerReview",
        status=final_state.status,
    )