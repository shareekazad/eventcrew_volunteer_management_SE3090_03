"""
EventCrew AI Service — FastAPI entry point.

This service hosts the Agentic AI subsystem: agents, tools, and the
LangGraph orchestration. It is called ONLY by the ASP.NET Core backend,
never directly by React or Flutter.
"""

import logging
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from app.agents.planning_agent import PlanResult, PlanningAgent
from app.tools.event_tools import EventSummary
from app.tools.venue_tools import VenueSummary


# ---------------------------------------------------------------------------
# Logging
# ---------------------------------------------------------------------------
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("eventcrew-ai")


# ---------------------------------------------------------------------------
# FastAPI application instance
# ---------------------------------------------------------------------------
app = FastAPI(
    title="EventCrew AI Service",
    description="Internal AI service for the EventCrew volunteer management platform.",
    version="0.1.0",
)


# ---------------------------------------------------------------------------
# Response / request models
# ---------------------------------------------------------------------------
class HealthResponse(BaseModel):
    status: str
    service: str
    version: str


class ServiceInfoResponse(BaseModel):
    name: str
    message: str


class PlanRequest(BaseModel):
    event_id: str = Field(..., description="UUID of the event to plan staffing for.")
    event: EventSummary
    venue: VenueSummary | None


# ---------------------------------------------------------------------------
# System routes
# ---------------------------------------------------------------------------
@app.get("/health", response_model=HealthResponse, tags=["system"])
def health_check() -> HealthResponse:
    """Simple health check. Used by ASP.NET Core to verify the AI service is up."""
    return HealthResponse(status="ok", service="eventcrew-ai", version="0.1.0")


@app.get("/", response_model=ServiceInfoResponse, tags=["system"])
def root() -> ServiceInfoResponse:
    """Root endpoint. Confirms the service is running."""
    return ServiceInfoResponse(
        name="EventCrew AI Service",
        message="Agentic AI subsystem is running. See /docs for the API.",
    )


# ---------------------------------------------------------------------------
# Agent routes
# ---------------------------------------------------------------------------
@app.post("/agent/plan", response_model=PlanResult, tags=["agent"])
async def plan_staffing(request: PlanRequest) -> PlanResult:
    """
    Run the LangGraph-orchestrated planning workflow against a given event.

    ASP.NET Core authorizes the caller and supplies only the required event and
    venue snapshot. The graph executes its allow-listed tools without receiving
    a JWT, password, backend credential, or other authentication secret.
    """
    logger.info("Received plan request for event_id=%s", request.event_id)

    try:
        result = await PlanningAgent().plan(
            request.event_id,
            request.event,
            request.venue,
        )
    except ValueError as e:
        logger.warning("Planning failed (validation) for event_id=%s: %s", request.event_id, e)
        raise HTTPException(status_code=400, detail=str(e))

    logger.info(
        "Plan complete: %d steps, %d tool calls, next_agent=%s",
        len(result.steps),
        len(result.tool_calls),
        result.next_agent,
    )
    return result