"""
EventCrew AI Service — FastAPI entry point.

This service hosts the Agentic AI subsystem: agents, tools, and the
LangGraph orchestration. It is called ONLY by the ASP.NET Core backend,
never directly by React or Flutter.
"""

import logging
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from app.agents.planning_agent import PlanResult
from app.graphs.planning_graph import run_planning_graph
from app.graphs.workflow_graph import run_workflow
from app.tools.http_client import BackendError


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
    version="0.2.0",
)


# ---------------------------------------------------------------------------
# Request / response models
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
    # Pre-fetched candidates from ASP.NET Core (avoids circular HTTP 401)
    candidates: list[dict] = Field(
        default_factory=list,
        description="Pre-fetched volunteer applications passed from ASP.NET Core."
    )


# ---------------------------------------------------------------------------
# System routes
# ---------------------------------------------------------------------------
@app.get("/health", response_model=HealthResponse, tags=["system"])
def health_check() -> HealthResponse:
    """Simple health check. Used by ASP.NET Core to verify the AI service is up."""
    return HealthResponse(status="ok", service="eventcrew-ai", version="0.2.0")


@app.get("/", response_model=ServiceInfoResponse, tags=["system"])
def root() -> ServiceInfoResponse:
    """Root endpoint. Confirms the service is running."""
    return ServiceInfoResponse(
        name="EventCrew AI Service",
        message="Agentic AI subsystem is running. See /docs for the API.",
    )


# ---------------------------------------------------------------------------
# Single-agent endpoint (PlanningAgent only) — LEGACY
# ---------------------------------------------------------------------------
@app.post("/agent/plan", response_model=PlanResult, tags=["agent"])
async def plan_staffing(request: PlanRequest) -> PlanResult:
    """
    [LEGACY] Run only the PlanningAgent.
    """
    logger.info("[legacy] Received single-agent plan request for event_id=%s", request.event_id)

    try:
        result = await run_planning_graph(request.event_id)
    except ValueError as e:
        logger.warning("Planning failed (validation) for event_id=%s: %s", request.event_id, e)
        raise HTTPException(status_code=400, detail=str(e))
    except BackendError as e:
        logger.error("Planning failed (backend) for event_id=%s: %s", request.event_id, e)
        raise HTTPException(status_code=503, detail=str(e))

    return result


# ---------------------------------------------------------------------------
# Full multi-agent workflow endpoint — RECOMMENDED
# ---------------------------------------------------------------------------
@app.post("/workflow/plan", tags=["workflow"])
async def workflow_plan(request: PlanRequest) -> dict:
    """
    Run the full 4-agent workflow:

        PlanningAgent → MatchingAgent → SchedulingAgent → ValidationAgent

    Accepts pre-fetched candidates from ASP.NET Core so the Python service
    does not need to make a circular HTTP call (which would return 401).
    """
    logger.info(
        "Received workflow request for event_id=%s (%d candidates supplied)",
        request.event_id, len(request.candidates)
    )

    try:
        result = await run_workflow(request.event_id, candidates=request.candidates)
    except ValueError as e:
        logger.warning("Workflow failed (validation) for event_id=%s: %s", request.event_id, e)
        raise HTTPException(status_code=400, detail=str(e))
    except BackendError as e:
        logger.error("Workflow failed (backend) for event_id=%s: %s", request.event_id, e)
        raise HTTPException(status_code=503, detail=str(e))
    except Exception as e:
        logger.exception("Workflow failed (unexpected) for event_id=%s", request.event_id)
        raise HTTPException(status_code=500, detail=f"Workflow error: {e}")

    logger.info(
        "Workflow complete: status=%s, agents=%d, traces=%d",
        result.get("status"),
        len({t.get("agent_name") for t in result.get("agent_traces", [])}),
        len(result.get("agent_traces", [])),
    )
    return result