"""
SchedulingAgent — Student 3's agent slot.

⚠️ NOTE ON OWNERSHIP:
This file was scaffolded by IT24104324 - Student 1  to unblock the group workflow,
because Student 3 had not yet delivered at integration time.
Student 3 owns this file — please extend with:
  - Domain-specific scheduling rules
  - Additional tools (e.g., volunteer preference weighting)
  - Unit tests

The framework (state, node wiring, orchestration) is shared and stable.
"""

"""
SchedulingAgent — Section 9.1.

Responsibility:
    Given matched candidates + event + role requirements, propose a
    concrete shift roster with time slots, capacities, and assignments.

Input contract:
    WorkflowState containing:
      - event (from PlanningAgent)
      - matching_results (from MatchingAgent)
      - plan_steps (from PlanningAgent — role requirements are embedded here)

Output contract:
    Updates WorkflowState with:
      - proposed_shifts: list of shift proposals
      - shift_conflicts: list of detected issues
      - agent_traces: appended audit entries

Controlled tools:
    - fetch_event_shifts
    - propose_shift_slots
    - check_shift_conflicts
    - log_agent_observability

Anything else is forbidden.
"""

import logging
import time
import uuid

from app.graphs.workflow_state import WorkflowState
from app.tools.matching_tools import log_agent_observability
from app.tools.scheduling_agent_tools import (
    SCHEDULING_ALLOWED_TOOLS,
    check_shift_conflicts,
    fetch_event_shifts,
    propose_shift_slots,
)


logger = logging.getLogger("eventcrew-ai.scheduling_agent")


class SchedulingAgent:
    """
    Scheduling Agent — proposes a shift roster from matched candidates.

    Distinct from:
    - PlanningAgent: does NOT decide headcounts (that's planning)
    - MatchingAgent: does NOT pick candidates (that's matching)
    - ValidationAgent: does NOT check for errors (that's validation)

    Its ONLY job is: turn matched candidates into a concrete shift plan.
    """

    name = "SchedulingAgent"
    ALLOWED_TOOLS = SCHEDULING_ALLOWED_TOOLS

    async def schedule(self, state: WorkflowState) -> dict:
        """
        Proposes a shift roster based on the current workflow state.

        Args:
            state: current WorkflowState with event, matching_results,
                   and plan_steps populated.

        Returns:
            Partial state update (dict) with:
              - proposed_shifts
              - shift_conflicts
              - agent_traces (appended)
        """
        t0 = time.perf_counter()
        workflow_id = state.workflow_id or str(uuid.uuid4())
        traces = list(state.agent_traces)  # copy, append, return new list

        # ------------------------------------------------------------------
        # Guard: need event + matching results to proceed
        # ------------------------------------------------------------------
        if state.event is None:
            return {
                "error": "SchedulingAgent: missing event in state.",
                "status": "failed",
            }

        if not state.matching_results:
            # No matches → nothing to schedule. This is a safe failure.
            logger.warning(
                "SchedulingAgent: no matching results for event %s. "
                "Cannot propose shifts.",
                state.event.id,
            )
            return {
                "proposed_shifts": [],
                "shift_conflicts": [{
                    "type": "no_candidates",
                    "severity": "high",
                    "message": "No matched candidates to schedule.",
                    "affected_ids": [],
                }],
                "agent_traces": traces + [{
                    "agent_name": self.name,
                    "tool_name": "precondition_check",
                    "input_params": {"event_id": state.event.id},
                    "output_summary": {"result": "no_matches"},
                    "duration_ms": 0,
                    "called_at": _now_iso(),
                }],
            }

        # ------------------------------------------------------------------
        # Tool 1: fetch existing shifts (to detect clashes against real data)
        # ------------------------------------------------------------------
        t_tool = time.perf_counter()
        existing_shifts = await fetch_event_shifts(state.event.id)
        duration_ms = int((time.perf_counter() - t_tool) * 1000)

        traces.append({
            "agent_name": self.name,
            "tool_name": "fetch_event_shifts",
            "input_params": {"event_id": state.event.id},
            "output_summary": {"existing_shifts": len(existing_shifts)},
            "duration_ms": duration_ms,
            "called_at": _now_iso(),
        })
        log_agent_observability(
            workflow_id=workflow_id,
            tool_name="fetch_event_shifts",
            input_params={"event_id": state.event.id},
            output_summary={"existing_shifts": len(existing_shifts)},
            duration_ms=duration_ms,
        )

        # ------------------------------------------------------------------
        # Extract role requirements from the workflow state
        # (they live in the PlanningAgent output — plan_steps has them)
        # ------------------------------------------------------------------
        role_requirements = _extract_role_requirements(state)

        # ------------------------------------------------------------------
        # Tool 2: propose shift slots
        # ------------------------------------------------------------------
        t_tool = time.perf_counter()
        proposed = propose_shift_slots(
            event_start_iso=state.event.start_date,
            event_end_iso=state.event.end_date,
            role_requirements=role_requirements,
            matching_results=state.matching_results,
        )
        duration_ms = int((time.perf_counter() - t_tool) * 1000)

        traces.append({
            "agent_name": self.name,
            "tool_name": "propose_shift_slots",
            "input_params": {
                "role_count": len(role_requirements),
                "matching_result_count": len(state.matching_results),
            },
            "output_summary": {"proposed_shifts": len(proposed)},
            "duration_ms": duration_ms,
            "called_at": _now_iso(),
        })
        log_agent_observability(
            workflow_id=workflow_id,
            tool_name="propose_shift_slots",
            input_params={"event_id": state.event.id},
            output_summary={"proposed_shifts": len(proposed)},
            duration_ms=duration_ms,
        )

        # ------------------------------------------------------------------
        # Tool 3: check for conflicts
        # ------------------------------------------------------------------
        t_tool = time.perf_counter()
        conflicts = check_shift_conflicts(proposed)
        duration_ms = int((time.perf_counter() - t_tool) * 1000)

        traces.append({
            "agent_name": self.name,
            "tool_name": "check_shift_conflicts",
            "input_params": {"shift_count": len(proposed)},
            "output_summary": {
                "conflicts": len(conflicts),
                "high_severity": sum(1 for c in conflicts if c["severity"] == "high"),
            },
            "duration_ms": duration_ms,
            "called_at": _now_iso(),
        })
        log_agent_observability(
            workflow_id=workflow_id,
            tool_name="check_shift_conflicts",
            input_params={"shift_count": len(proposed)},
            output_summary={"conflicts": len(conflicts)},
            duration_ms=duration_ms,
        )

        # ------------------------------------------------------------------
        # Failure handling: if any HIGH severity conflict, mark failed
        # ------------------------------------------------------------------
        high_conflicts = [c for c in conflicts if c["severity"] == "high"]
        if high_conflicts:
            logger.warning(
                "SchedulingAgent: %d high-severity conflicts found. Marking as failed.",
                len(high_conflicts),
            )
            return {
                "proposed_shifts": proposed,
                "shift_conflicts": conflicts,
                "agent_traces": traces,
                "error": f"Scheduling produced {len(high_conflicts)} high-severity conflict(s).",
                "status": "failed",
            }

        total_ms = int((time.perf_counter() - t0) * 1000)
        logger.info(
            "SchedulingAgent completed: %d shifts proposed, %d conflicts, %dms",
            len(proposed), len(conflicts), total_ms,
        )

        return {
            "proposed_shifts": proposed,
            "shift_conflicts": conflicts,
            "agent_traces": traces,
        }


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
def _now_iso() -> str:
    from datetime import datetime, timezone
    return datetime.now(timezone.utc).isoformat()


def _extract_role_requirements(state: WorkflowState) -> list[dict]:
    """
    Extracts role requirements from the workflow state.

    Role requirements are stored inside the plan_steps produced by
    the PlanningAgent. If the plan doesn't include them explicitly,
    we can fetch them from the event's role_requirements.
    """
    # Preferred source: role requirements embedded in the plan
    # (PlanningAgent stores them in the event object from get_event)
    if state.event and hasattr(state.event, "role_requirements"):
        roles = []
        for r in state.event.role_requirements:
            roles.append({
                "id": getattr(r, "id", None),
                "roleName": getattr(r, "role_name", None),
                "requiredHeadcount": getattr(r, "required_headcount", None),
                "minExperienceLevel": getattr(r, "min_experience_level", None),
            })
        if roles:
            return roles

    # Fallback: no roles in the plan
    return []