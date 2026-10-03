"""
Volunteer Matching Agent — Section 9.1 & 12 Specification.

Autonomous agent responsible for evaluating event volunteer applicants against role requirements
(required skills, experience tier, past rating score) and producing a ranked list of candidate
matches with transparent justifications, strictly using allow-listed tools and guardrails.
"""

import logging
import re
import time
import uuid
from uuid import UUID

from app.schemas.matching_schema import (
    CandidateMatch,
    MatchingRequest,
    MatchingResponse,
)
from app.tools.matching_tools import (
    compute_skill_affinity_score,
    fetch_eligible_applicants,
    log_agent_observability,
)


logger = logging.getLogger("eventcrew-ai.matching_agent")

# ---------------------------------------------------------------------------
# Guardrail 1: Prompt Injection & Sanitization Patterns
# ---------------------------------------------------------------------------
_PROMPT_INJECTION_REGEX = re.compile(
    r"(?i)\b("
    r"ignore\s+(all\s+)?(previous|prior|above)\s+instructions|"
    r"disregard\s+(all\s+)?(previous|prior|above|earlier)\s+instructions|"
    r"system\s*prompt\s*:|"
    r"you\s+are\s+now\s+(a|an)\b|"
    r"admin\s+override|"
    r"override\s+(match_)?score|"
    r"output\s+the\s+flag|"
    r"drop\s+table\b"
    r")"
)
_TAG_DELIMITER_REGEX = re.compile(r"<[^>]+>|```|---|###|===")


def sanitize_input_text(text: str | None) -> str:
    """
    Guardrail 1: Strips out malicious instruction overrides, prompt injections,
    dangerous delimiters, and script/markup tags from applicant-provided strings.
    """
    if not text:
        return ""

    # 1. Strip HTML/XML/markdown delimiter tags
    cleaned = _TAG_DELIMITER_REGEX.sub(" ", text)

    # 2. Redact prompt injection directives
    cleaned = _PROMPT_INJECTION_REGEX.sub("[REDACTED_INJECTION]", cleaned)

    # 3. Normalize whitespace
    return " ".join(cleaned.split()).strip()


# ---------------------------------------------------------------------------
# Matching Agent
# ---------------------------------------------------------------------------
class VolunteerMatchingAgent:
    """
    Volunteer Matching Agent.

    Evaluates applicant pool using deterministic affinity scoring,
    enforcing guardrails (sanitization, advanced role constraints, safe failure).
    """

    name = "VolunteerMatchingAgent"

    # Section 9.1: Controlled Allow-Listed Tools
    ALLOWED_TOOLS = {
        "fetch_eligible_applicants",
        "compute_skill_affinity_score",
        "log_agent_observability",
    }

    async def match(
        self,
        request: MatchingRequest,
        mock_applicants: list[dict] | None = None,
    ) -> MatchingResponse:
        """
        Executes the volunteer matching workflow for the given event role.

        Args:
            request: Validated MatchingRequest containing event_id, role_name,
                     required_skills, min_experience_level, and required_headcount.
            mock_applicants: Optional pre-seeded applicant records for testing.

        Returns:
            MatchingResponse containing ranked candidate matches, status,
            unfulfilled slots, and execution metrics.
        """
        t0 = time.perf_counter()
        workflow_id = uuid.uuid4()
        event_id_str = str(request.event_id)

        logger.info(
            "Starting matching workflow %s for event=%s, role='%s', headcount=%d",
            workflow_id, event_id_str, request.role_name, request.required_headcount
        )

        # -------------------------------------------------------------------
        # Tool 1: Fetch eligible applicants
        # -------------------------------------------------------------------
        t_tool1_start = time.perf_counter()
        raw_applicants = await fetch_eligible_applicants(event_id_str, mock_applicants)
        t_tool1_duration = int((time.perf_counter() - t_tool1_start) * 1000)

        log_agent_observability(
            workflow_id=str(workflow_id),
            tool_name="fetch_eligible_applicants",
            input_params={"event_id": event_id_str},
            output_summary={"applicants_retrieved": len(raw_applicants)},
            duration_ms=t_tool1_duration,
        )

        # -------------------------------------------------------------------
        # Process and evaluate each candidate
        # -------------------------------------------------------------------
        candidate_evaluations: list[CandidateMatch] = []
        t_scoring_start = time.perf_counter()

        for raw_app in raw_applicants:
            # Guardrail 1: Sanitize volunteer name and notes against prompt injection
            raw_name = raw_app.get("volunteer_name") or f"Volunteer {str(raw_app.get('volunteer_id', ''))[:8]}"
            sanitized_name = sanitize_input_text(raw_name) or "Volunteer"
            sanitized_notes = sanitize_input_text(raw_app.get("notes", ""))

            # Validate volunteer_id
            raw_vid = raw_app.get("volunteer_id")
            try:
                vol_uuid = UUID(str(raw_vid))
            except (ValueError, TypeError, AttributeError):
                logger.warning("Skipping applicant with invalid UUID: %s", raw_vid)
                continue

            raw_rating = float(raw_app.get("rating_score", 5.0))
            if raw_rating < 0.0 or raw_rating > 5.0:
                raise ValueError(f"Rating score must be between 0.0 and 5.0, got {raw_rating}")

            vol_skills = raw_app.get("skills", [])
            vol_experience = raw_app.get("experience_level", "Beginner")

            # Tool 2: Compute deterministic skill affinity score
            affinity = compute_skill_affinity_score(
                volunteer_skills=vol_skills,
                required_skills=request.required_skills,
                rating=raw_rating,
                volunteer_experience_level=vol_experience,
                min_experience_level=request.min_experience_level,
            )

            # Guardrail 2: Candidates with 0% skill match cannot be assigned
            # to roles requiring "Advanced" experience
            is_advanced_role = request.min_experience_level.strip().lower() == "advanced"
            has_zero_skill_overlap = (
                bool(request.required_skills)
                and len(affinity["matching_skills"]) == 0
            )

            if is_advanced_role and has_zero_skill_overlap:
                logger.info(
                    "Guardrail 2 triggered: volunteer %s disqualified from Advanced role '%s' due to 0%% skill match.",
                    vol_uuid, request.role_name
                )
                continue

            # Construct transparent justification
            matching_skills = affinity["matching_skills"]
            overlap_pct = affinity["skill_overlap_percentage"]

            if request.required_skills:
                if overlap_pct >= 99.9:
                    justification = (
                        f"Strong candidate with 100% required skill overlap "
                        f"({', '.join(matching_skills)}), past rating {raw_rating:.1f}/5.0, "
                        f"and {vol_experience} tier alignment for '{request.role_name}'."
                    )
                elif overlap_pct > 0:
                    justification = (
                        f"Qualified candidate possessing {overlap_pct:.0f}% of required skills "
                        f"({', '.join(matching_skills)}), rating {raw_rating:.1f}/5.0, "
                        f"and {vol_experience} experience tier."
                    )
                else:
                    justification = (
                        f"Assigned candidate with rating {raw_rating:.1f}/5.0 and "
                        f"{vol_experience} experience tier."
                    )
            else:
                justification = (
                    f"Candidate assigned based on past rating {raw_rating:.1f}/5.0 "
                    f"and {vol_experience} experience tier."
                )

            if sanitized_notes:
                justification += f" Note: {sanitized_notes[:80]}"

            candidate_evaluations.append(
                CandidateMatch(
                    volunteer_id=vol_uuid,
                    volunteer_name=sanitized_name,
                    match_score=affinity["match_score"],
                    matching_skills=matching_skills,
                    experience_level=vol_experience,
                    rating_score=raw_rating,
                    justification=justification,
                )
            )

        t_scoring_duration = int((time.perf_counter() - t_scoring_start) * 1000)

        log_agent_observability(
            workflow_id=str(workflow_id),
            tool_name="compute_skill_affinity_score",
            input_params={
                "role_name": request.role_name,
                "required_skills": request.required_skills,
                "min_experience_level": request.min_experience_level,
                "applicants_evaluated": len(raw_applicants),
            },
            output_summary={
                "qualified_candidates": len(candidate_evaluations),
                "scoring_duration_ms": t_scoring_duration,
            },
            duration_ms=t_scoring_duration,
        )

        # -------------------------------------------------------------------
        # Deterministic Ranking
        # Highest match score first, with rating_score as secondary tiebreaker
        # -------------------------------------------------------------------
        candidate_evaluations.sort(
            key=lambda c: (c.match_score, c.rating_score),
            reverse=True
        )

        # -------------------------------------------------------------------
        # Guardrail 3: Safe Failure & Outcome Determination
        # -------------------------------------------------------------------
        target_headcount = request.required_headcount

        if len(candidate_evaluations) == 0:
            # Guardrail 3: Never hallucinate fake volunteers when 0 qualify
            status = "SAFE_FAILURE"
            matched_candidates: list[CandidateMatch] = []
            unfulfilled_slots = target_headcount
            logger.warning(
                "Guardrail 3 triggered: SAFE_FAILURE for role '%s'. 0 qualified candidates found. 0 fake candidates generated.",
                request.role_name
            )
        elif len(candidate_evaluations) < target_headcount:
            status = "PARTIAL_MATCH"
            matched_candidates = candidate_evaluations
            unfulfilled_slots = target_headcount - len(candidate_evaluations)
        else:
            status = "SUCCESS"
            matched_candidates = candidate_evaluations[:target_headcount]
            unfulfilled_slots = 0

        total_execution_ms = int((time.perf_counter() - t0) * 1000)

        # Final observability log
        log_agent_observability(
            workflow_id=str(workflow_id),
            tool_name="workflow_completion",
            input_params={"headcount_needed": target_headcount},
            output_summary={
                "status": status,
                "matched_count": len(matched_candidates),
                "unfulfilled_slots": unfulfilled_slots,
            },
            duration_ms=total_execution_ms,
        )

        return MatchingResponse(
            workflow_id=workflow_id,
            role_name=request.role_name,
            headcount_needed=target_headcount,
            matched_candidates=matched_candidates,
            unfulfilled_slots=unfulfilled_slots,
            execution_time_ms=total_execution_ms,
            status=status,
        )


# Section 9.1 Alias
MatchingAgent = VolunteerMatchingAgent
