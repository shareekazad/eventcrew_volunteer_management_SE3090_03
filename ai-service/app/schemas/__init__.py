"""Pydantic schemas for the AI service."""
from app.schemas.matching_schema import (
    MatchingRequest,
    MatchingResponse,
    CandidateMatch,
)

__all__ = [
    "MatchingRequest",
    "MatchingResponse",
    "CandidateMatch",
]
