"""
Matching Router — REST API endpoint for the Volunteer Matching Agent.

Exposes POST /api/agents/match-volunteers called by ASP.NET Core backend.
"""

import logging
from fastapi import APIRouter, HTTPException, status

from app.agents.matching_agent import VolunteerMatchingAgent
from app.schemas.matching_schema import MatchingRequest, MatchingResponse
from app.tools.http_client import BackendError


logger = logging.getLogger("eventcrew-ai.matching_router")

matching_router = APIRouter(prefix="/api/agents", tags=["matching-agent"])
_agent = VolunteerMatchingAgent()


@matching_router.post(
    "/match-volunteers",
    response_model=MatchingResponse,
    status_code=status.HTTP_200_OK,
    summary="Match and rank volunteer applicants for an event role",
    description=(
        "Evaluates event applicants against required skills, experience level, and ratings. "
        "Strictly called by the ASP.NET Core backend (never directly by frontend)."
    ),
)
async def match_volunteers(request: MatchingRequest) -> MatchingResponse:
    """
    Executes the Volunteer Matching Agent workflow with deterministic scoring,
    guardrails (sanitization, advanced skill requirements, safe failure),
    and observability logging.
    """
    logger.info(
        "Received match-volunteers request: event=%s, role='%s', headcount=%d",
        request.event_id, request.role_name, request.required_headcount
    )

    try:
        response = await _agent.match(request)
        return response
    except ValueError as exc:
        logger.warning(
            "Matching validation error for event=%s: %s",
            request.event_id, exc
        )
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=str(exc)
        ) from exc
    except BackendError as exc:
        logger.error(
            "Backend communication error for event=%s: %s",
            request.event_id, exc
        )
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail=str(exc)
        ) from exc
    except Exception as exc:
        logger.exception("Unexpected error in volunteer matching agent: %s", exc)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="An internal error occurred during volunteer matching."
        ) from exc
