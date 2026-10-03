"""
Matching Schema Contracts — Section 9.1 & Section 12 Specification.

Defines the strict Pydantic input and output models for the Volunteer Matching Agent.
"""

from typing import Literal
from uuid import UUID
from pydantic import BaseModel, Field, field_validator


ExperienceTier = Literal["Beginner", "Intermediate", "Advanced"]
MatchingStatus = Literal["SUCCESS", "PARTIAL_MATCH", "SAFE_FAILURE"]


class MatchingRequest(BaseModel):
    """
    Input contract for requesting automated volunteer applicant matching.
    """
    event_id: UUID = Field(
        ...,
        description="UUID of the event requiring volunteer staffing."
    )
    role_name: str = Field(
        ...,
        min_length=1,
        description="Name of the volunteer role (e.g., 'Emergency Response Team')."
    )
    required_skills: list[str] = Field(
        default_factory=list,
        description="List of skills required for the role (e.g., ['First Aid', 'CPR'])."
    )
    min_experience_level: ExperienceTier = Field(
        default="Beginner",
        description="Minimum experience tier required ('Beginner', 'Intermediate', or 'Advanced')."
    )
    required_headcount: int = Field(
        ...,
        gt=0,
        description="Target number of volunteers to assign to this role (> 0)."
    )

    @field_validator("min_experience_level", mode="before")
    @classmethod
    def normalize_experience_level(cls, value: str) -> str:
        if isinstance(value, str):
            val_lower = value.strip().lower()
            if val_lower == "beginner":
                return "Beginner"
            if val_lower == "intermediate":
                return "Intermediate"
            if val_lower == "advanced":
                return "Advanced"
        return value


class CandidateMatch(BaseModel):
    """
    Ranked match details for a single qualified volunteer candidate.
    """
    volunteer_id: UUID = Field(
        ...,
        description="UUID of the matched volunteer."
    )
    volunteer_name: str = Field(
        ...,
        description="Sanitized display name of the volunteer."
    )
    match_score: float = Field(
        ...,
        ge=0.0,
        le=100.0,
        description="Composite affinity score (0.0 to 100.0)."
    )
    matching_skills: list[str] = Field(
        default_factory=list,
        description="Subset of required skills possessed by the volunteer."
    )
    experience_level: str = Field(
        ...,
        description="Volunteer self-declared experience tier."
    )
    rating_score: float = Field(
        ...,
        ge=0.0,
        le=5.0,
        description="Past feedback rating on a 0.00 – 5.00 scale."
    )
    justification: str = Field(
        ...,
        description="Clear, transparent explanation of why this volunteer is a strong fit."
    )


class MatchingResponse(BaseModel):
    """
    Output contract returned by the Volunteer Matching Agent.
    """
    workflow_id: UUID = Field(
        ...,
        description="Unique execution run identifier for observability and audit logging."
    )
    role_name: str = Field(
        ...,
        description="Target role for which matching was performed."
    )
    headcount_needed: int = Field(
        ...,
        ge=0,
        description="Total headcount originally requested."
    )
    matched_candidates: list[CandidateMatch] = Field(
        default_factory=list,
        description="Ranked list of matched candidates meeting qualification criteria."
    )
    unfulfilled_slots: int = Field(
        ...,
        ge=0,
        description="Number of slots remaining unfilled due to candidate shortage or qualification failure."
    )
    execution_time_ms: int = Field(
        ...,
        ge=0,
        description="Total agent execution duration in milliseconds."
    )
    status: MatchingStatus = Field(
        ...,
        description="Overall match outcome: 'SUCCESS', 'PARTIAL_MATCH', or 'SAFE_FAILURE'."
    )
