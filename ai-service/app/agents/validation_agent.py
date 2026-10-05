"""
ValidationAgent — Student 4's agent slot.

⚠️ NOTE ON OWNERSHIP:
Scaffolded by Student 1 to unblock the group workflow.
Student 4 owns this file — please extend with:
  - Domain-specific business rules
  - Additional validators (venue capacity, skill coverage %, etc.)
  - Unit tests for each rule

The framework (state, node wiring, orchestration) is shared and stable.
"""

"""
ValidationAgent — Section 9.1.

Responsibility:
    Given a proposed shift roster + event details + role requirements,
    run deterministic checks. Produce a structured validation report.

Input contract:
    WorkflowState containing:
      - event (from PlanningAgent)
      - proposed_shifts (from SchedulingAgent)
      - shift_conflicts (from SchedulingAgent)

Output contract:
    Updates WorkflowState with:
      - validation_passed: bool
      - validation_errors: list[str]
      - validation_warnings: list[str]
      - agent_traces: appended audit entries

Controlled tools:
    - assert_no_double_booking
    - assert_headcount_met
    - assert_experience_match
    - assert_shift_duration
    - assert_event_bounds
    - log_agent_observability

This agent NEVER mutates the roster. It only reports. Anything else is forbidden.
"""

import logging
import time
import uuid
from datetime import datetime, timezone

from app.graphs.workflow_state import WorkflowState
from app.tools.matching_tools import log_agent_observability
from app.tools.validation_agent_tools import (
    VALIDATION_ALLOWED_TOOLS,
    assert_event_bounds,
    assert_experience_match,
    assert_headcount_met,
    assert_no_double_booking,
    assert_shift_duration,
)


logger = logging.getLogger("eventcrew-ai.validation_agent")


class ValidationAgent:
    """
    Validation Agent — applies deterministic checks to the proposed roster.

    Distinct from:
    - PlanningAgent: doesn't decide headcounts
    - MatchingAgent: doesn't pick candidates
    - SchedulingAgent: doesn't propose shifts

    Its ONLY job is: report on the correctness of the proposed roster.
    """

    name = "ValidationAgent"
    ALLOWED_TOOLS = VALIDATION_ALLOWED_TOOLS

    async def validate(self, state: WorkflowState) -> dict:
        """
        Runs all validation rules against the current workflow state.

        Returns:
            Partial state update (dict) with:
              - validation_passed
              - validation_errors
              - validation_warnings
              - agent_traces (appended)
        """
        t0 = time.perf_counter()
        workflow_id = state.workflow_id or str(uuid.uuid4())
        traces = list(state.agent_traces)

        errors: list[str] = []
        warnings: list[str] = []

        # ------------------------------------------------------------------
        # Precondition: need a proposal to validate
        # ------------------------------------------------------------------
        if state.event is None:
            return {
                "error": "ValidationAgent: missing event in state.",
                "status": "failed",
            }

        if not state.proposed_shifts:
            # No shifts proposed — that's a hard failure
            logger.warning(
                "ValidationAgent: no proposed shifts for event %s.",
                state.event.id,
            )
            return {
                "validation_passed": False,
                "validation_errors": ["No shifts were proposed."],
                "validation_warnings": [],
                "agent_traces": traces + [{
                    "agent_name": self.name,
                    "tool_name": "precondition_check",
                    "input_params": {"event_id": state.event.id},
                    "output_summary": {"result": "no_shifts"},
                    "duration_ms": 0,
                    "called_at": _now_iso(),
                }],
            }

        role_requirements = _extract_role_requirements(state)

        # ------------------------------------------------------------------
        # Rule 1: no double-booking
        # ------------------------------------------------------------------
        _run_rule(
            rule_name="assert_no_double_booking",
            fn=lambda: assert_no_double_booking(state.proposed_shifts),
            traces=traces,
            workflow_id=workflow_id,
            errors=errors,
            input_params={"shift_count": len(state.proposed_shifts)},
        )

        # ------------------------------------------------------------------
        # Rule 2: headcount met
        # ------------------------------------------------------------------
        _run_rule(
            rule_name="assert_headcount_met",
            fn=lambda: assert_headcount_met(state.proposed_shifts, role_requirements),
            traces=traces,
            workflow_id=workflow_id,
            errors=errors,
            input_params={"role_count": len(role_requirements)},
        )

        # ------------------------------------------------------------------
        # Rule 3: experience match
        # ------------------------------------------------------------------
        _run_rule(
            rule_name="assert_experience_match",
            fn=lambda: assert_experience_match(state.proposed_shifts, role_requirements),
            traces=traces,
            workflow_id=workflow_id,
            errors=errors,
            input_params={"role_count": len(role_requirements)},
        )

        # ------------------------------------------------------------------
        # Rule 4: shift duration
        # ------------------------------------------------------------------
        _run_rule(
            rule_name="assert_shift_duration",
            fn=lambda: assert_shift_duration(state.proposed_shifts),
            traces=traces,
            workflow_id=workflow_id,
            errors=errors,
            input_params={"shift_count": len(state.proposed_shifts)},
        )

        # ------------------------------------------------------------------
        # Rule 5: event bounds
        # ------------------------------------------------------------------
        _run_rule(
            rule_name="assert_event_bounds",
            fn=lambda: assert_event_bounds(
                state.proposed_shifts,
                state.event.start_date,
                state.event.end_date,
            ),
            traces=traces,
            workflow_id=workflow_id,
            errors=errors,
            input_params={"shift_count": len(state.proposed_shifts)},
        )

        # ------------------------------------------------------------------
        # Rule 6 (soft): use scheduling conflicts as warnings
        # ------------------------------------------------------------------
        if state.shift_conflicts:
            for c in state.shift_conflicts:
                if c.get("severity") == "low":
                    warnings.append(c.get("message", "Low severity conflict."))
                elif c.get("severity") == "medium":
                    warnings.append(c.get("message", "Medium severity conflict."))

        # ------------------------------------------------------------------
        # Verdict
        # ------------------------------------------------------------------
        passed = len(errors) == 0
        total_ms = int((time.perf_counter() - t0) * 1000)

        logger.info(
            "ValidationAgent completed in %dms: passed=%s, errors=%d, warnings=%d",
            total_ms, passed, len(errors), len(warnings),
        )

        return {
            "validation_passed": passed,
            "validation_errors": errors,
            "validation_warnings": warnings,
            "agent_traces": traces,
        }


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _run_rule(
    rule_name: str,
    fn,
    traces: list[dict],
    workflow_id: str,
    errors: list[str],
    input_params: dict,
) -> None:
    """
    Runs one validation rule, times it, logs it, and appends any errors.
    """
    t = time.perf_counter()
    try:
        rule_errors = fn()
    except Exception as exc:
        rule_errors = [f"{rule_name} crashed: {exc}"]
    duration_ms = int((time.perf_counter() - t) * 1000)

    traces.append({
        "agent_name": "ValidationAgent",
        "tool_name": rule_name,
        "input_params": input_params,
        "output_summary": {"errors": len(rule_errors)},
        "duration_ms": duration_ms,
        "called_at": _now_iso(),
    })

    log_agent_observability(
        workflow_id=workflow_id,
        tool_name=rule_name,
        input_params=input_params,
        output_summary={"errors": len(rule_errors)},
        duration_ms=duration_ms,
    )

    errors.extend(rule_errors)


def _extract_role_requirements(state: WorkflowState) -> list[dict]:
    """Same helper as in SchedulingAgent — pulls roles from the event."""
    if state.event and hasattr(state.event, "role_requirements"):
        roles = []
        for r in state.event.role_requirements:
            roles.append({
                "id": getattr(r, "id", None),
                "roleName": getattr(r, "role_name", None),
                "requiredHeadcount": getattr(r, "required_headcount", None),
                "minExperienceLevel": getattr(r, "min_experience_level", None),
            })
        return roles
    return []