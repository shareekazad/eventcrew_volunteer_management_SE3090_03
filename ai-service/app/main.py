"""
EventCrew AI Service — FastAPI entry point.

This service hosts the Agentic AI subsystem: agents, tools, and the
LangGraph orchestration. It is called ONLY by the ASP.NET Core backend,
never directly by React or Flutter.
"""

from fastapi import FastAPI
from pydantic import BaseModel


# ---------------------------------------------------------------------------
# FastAPI application instance
# ---------------------------------------------------------------------------
app = FastAPI(
    title="EventCrew AI Service",
    description="Internal AI service for the EventCrew volunteer management platform.",
    version="0.1.0",
)


# ---------------------------------------------------------------------------
# Response models (Pydantic — Python's equivalent of C# DTOs)
# ---------------------------------------------------------------------------
class HealthResponse(BaseModel):
    status: str
    service: str
    version: str


class ServiceInfoResponse(BaseModel):
    name: str
    message: str


# ---------------------------------------------------------------------------
# Routes
# ---------------------------------------------------------------------------
@app.get("/health", response_model=HealthResponse, tags=["system"])
def health_check() -> HealthResponse:
    """
    Simple health check. Used by ASP.NET Core to verify the AI service is up.
    """
    return HealthResponse(status="ok", service="eventcrew-ai", version="0.1.0")


@app.get("/", response_model=ServiceInfoResponse, tags=["system"])
def root() -> ServiceInfoResponse:
    """
    Root endpoint. Confirms the service is running.
    """
    return ServiceInfoResponse(
        name="EventCrew AI Service",
        message="Agentic AI subsystem is running. See /docs for the API.",
    )