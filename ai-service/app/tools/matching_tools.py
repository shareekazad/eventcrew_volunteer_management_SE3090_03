"""
Controlled Allow-Listed Tools for Volunteer Matching Agent.

Implements the three Section 9.1 tools:
1. fetch_eligible_applicants
2. compute_skill_affinity_score
3. log_agent_observability
"""

import logging
import uuid
from datetime import datetime, timezone
from typing import Any
from app.tools.http_client import BackendClient, BackendError


logger = logging.getLogger("eventcrew-ai.matching_tools")

# In-memory registry for mock/testing seed data
_MOCK_APPLICANTS: dict[str, list[dict]] = {}
_OBSERVABILITY_LOGS: list[dict] = []

EXPERIENCE_TIERS = {
    "beginner": 1,
    "intermediate": 2,
    "advanced": 3,
}


def seed_mock_applicants(event_id: str, applicants: list[dict]) -> None:
    """Helper to seed applicants for unit and integration testing."""
    _MOCK_APPLICANTS[str(event_id)] = applicants


def clear_mock_applicants() -> None:
    """Helper to clear test-seeded applicants."""
    _MOCK_APPLICANTS.clear()


def get_observability_logs() -> list[dict]:
    """Retrieve recorded observability audit entries."""
    return list(_OBSERVABILITY_LOGS)


def clear_observability_logs() -> None:
    """Clear recorded observability audit entries."""
    _OBSERVABILITY_LOGS.clear()


# ---------------------------------------------------------------------------
# Tool 1: fetch_eligible_applicants
# ---------------------------------------------------------------------------
async def fetch_eligible_applicants(
    event_id: str,
    mock_applicants: list[dict] | None = None
) -> list[dict]:
    """
    Retrieves submitted and under-review applicants for an event along with
    their volunteer profiles, skills, and ratings.

    Args:
        event_id: UUID of the event as string.
        mock_applicants: Optional pre-loaded applicant list for testing.

    Returns:
        List of applicant dictionaries containing:
        - applicant_id: str
        - volunteer_id: str
        - volunteer_name: str
        - status: str ('Submitted' | 'UnderReview')
        - experience_level: str
        - skills: list[str]
        - rating_score: float
        - notes: str
    """
    event_key = str(event_id).strip()

    # 1. Use explicitly passed applicants or in-memory seed if available
    if mock_applicants is not None:
        return mock_applicants
    if event_key in _MOCK_APPLICANTS:
        return _MOCK_APPLICANTS[event_key]

    # 2. Query ASP.NET Core backend when running in production/docker
    try:
        async with BackendClient() as client:
            apps_data = await client.get(f"/api/applications/event/{event_key}")
            if not apps_data or not isinstance(apps_data, list):
                return []

            eligible_apps: list[dict] = []
            for item in apps_data:
                app_status = item.get("status", "")
                if app_status not in ("Submitted", "UnderReview"):
                    continue

                vol_id = item.get("volunteerId")
                if not vol_id:
                    continue

                # Fetch profile details for skills & ratings
                profile_data = await client.get(f"/api/volunteers/{vol_id}")
                skills_list: list[str] = []
                rating = 5.0
                exp_level = "Intermediate"
                vol_name = f"Volunteer {str(vol_id)[:8]}"

                if profile_data and isinstance(profile_data, dict):
                    skills_list = [
                        s.get("name", "")
                        for s in profile_data.get("skills", [])
                        if s.get("name")
                    ]
                    rating = float(profile_data.get("ratingScore", 5.0))
                    bio = profile_data.get("bio", "")
                    if "beginner" in bio.lower():
                        exp_level = "Beginner"
                    elif "advanced" in bio.lower():
                        exp_level = "Advanced"

                eligible_apps.append({
                    "applicant_id": str(item.get("id", uuid.uuid4())),
                    "volunteer_id": str(vol_id),
                    "volunteer_name": vol_name,
                    "status": app_status,
                    "experience_level": exp_level,
                    "skills": skills_list,
                    "rating_score": rating,
                    "notes": item.get("notes") or "",
                })

            return eligible_apps

    except (BackendError, Exception) as exc:
        logger.warning(
            "Could not fetch applicants from backend for event %s (fallback to empty): %s",
            event_key, exc
        )
        return []


# ---------------------------------------------------------------------------
# Tool 2: compute_skill_affinity_score
# ---------------------------------------------------------------------------
def compute_skill_affinity_score(
    volunteer_skills: list[str],
    required_skills: list[str],
    rating: float,
    volunteer_experience_level: str = "Beginner",
    min_experience_level: str = "Beginner",
) -> dict:
    """
    Deterministic scoring algorithm per Section 9.1:
    - Overlap percentage of required skills (60% weight)
    - Volunteer average rating score normalized (30% weight)
    - Experience tier alignment (10% weight)

    Args:
        volunteer_skills: Skills possessed by the candidate.
        required_skills: Skills required by the role.
        rating: Past rating score (0.0 to 5.0).
        volunteer_experience_level: 'Beginner', 'Intermediate', or 'Advanced'.
        min_experience_level: Minimum required tier.

    Returns:
        dict with match_score (0.0-100.0), matching_skills, overlap %, normalized rating, tier alignment.

    Raises:
        ValueError: If rating is negative or > 5.0.
    """
    if rating < 0.0 or rating > 5.0:
        raise ValueError(f"Rating score must be between 0.0 and 5.0, got {rating}")

    # 1. Overlap percentage of required skills (60% weight)
    if not required_skills:
        overlap_pct = 100.0
        matching = list(volunteer_skills)
    else:
        req_clean = [s.strip() for s in required_skills if s and s.strip()]
        vol_set = {s.strip().lower() for s in volunteer_skills if s and s.strip()}
        
        matching = [
            req for req in req_clean
            if req.lower() in vol_set
        ]
        overlap_pct = (len(matching) / len(req_clean)) * 100.0 if req_clean else 100.0

    skill_weighted = 0.60 * overlap_pct

    # 2. Volunteer average rating score normalized (30% weight)
    # Rating scale: 0.00 – 5.00 -> normalized to 0.0 – 100.0%
    normalized_rating = (rating / 5.0) * 100.0
    rating_weighted = 0.30 * normalized_rating

    # 3. Experience tier alignment (10% weight)
    vol_tier = EXPERIENCE_TIERS.get(str(volunteer_experience_level).strip().lower(), 1)
    min_tier = EXPERIENCE_TIERS.get(str(min_experience_level).strip().lower(), 1)

    if vol_tier >= min_tier:
        exp_alignment = 100.0
    else:
        exp_alignment = (vol_tier / min_tier) * 100.0

    exp_weighted = 0.10 * exp_alignment

    total_score = round(skill_weighted + rating_weighted + exp_weighted, 2)
    clamped_score = max(0.0, min(100.0, total_score))

    return {
        "match_score": clamped_score,
        "matching_skills": matching,
        "skill_overlap_percentage": round(overlap_pct, 2),
        "normalized_rating": round(normalized_rating, 2),
        "experience_alignment_score": round(exp_alignment, 2),
    }


# ---------------------------------------------------------------------------
# Tool 3: log_agent_observability
# ---------------------------------------------------------------------------
def log_agent_observability(
    workflow_id: str,
    tool_name: str,
    input_params: dict,
    output_summary: dict,
    duration_ms: int,
) -> dict:
    """
    Records an execution trace entry for the PostgreSQL agent_tool_logs table.

    Args:
        workflow_id: Workflow execution UUID string.
        tool_name: Name of tool executed.
        input_params: Input arguments provided to the tool.
        output_summary: Concise structured output or metrics.
        duration_ms: Execution duration in milliseconds.

    Returns:
        Structured audit log entry dictionary.
    """
    entry = {
        "id": str(uuid.uuid4()),
        "workflow_run_id": str(workflow_id),
        "agent_name": "VolunteerMatchingAgent",
        "tool_name": str(tool_name),
        "input_parameters": input_params,
        "output_summary": output_summary,
        "execution_duration_ms": max(0, int(duration_ms)),
        "called_at": datetime.now(timezone.utc).isoformat(),
    }
    _OBSERVABILITY_LOGS.append(entry)
    logger.info(
        "Agent Observability Log: tool=%s, workflow=%s, duration=%dms",
        tool_name, workflow_id, duration_ms
    )
    return entry
