"""
Tests for the deterministic staffing-ratio tool.

These are pure-function tests — no HTTP, no DB, no mocking.
"""

import pytest

from app.tools.scheduling_tools import calculate_staffing_ratio


# ---------------------------------------------------------------------------
# Happy path
# ---------------------------------------------------------------------------
def test_calculate_staffing_ratio_typical_capacity():
    result = calculate_staffing_ratio(500)
    # 500/100 = 5 ushers, 500/300 ≈ 2 registration
    assert result.venue_capacity == 500
    assert result.recommended_ushers == 5
    assert result.recommended_registration_staff == 2
    assert result.total_staff == 7


def test_calculate_staffing_ratio_large_capacity():
    result = calculate_staffing_ratio(2500)
    # 2500/100 = 25 ushers, 2500/300 ≈ 9 registration
    assert result.recommended_ushers == 25
    assert result.recommended_registration_staff == 9
    assert result.total_staff == 34


# ---------------------------------------------------------------------------
# Minimums
# ---------------------------------------------------------------------------
def test_calculate_staffing_ratio_minimum_ushers():
    """Very small venue still gets minimum 2 ushers and 1 registration."""
    result = calculate_staffing_ratio(10)
    assert result.recommended_ushers == 2   # min 2
    assert result.recommended_registration_staff == 1  # min 1
    assert result.total_staff == 3


def test_calculate_staffing_ratio_boundary_capacity_100():
    result = calculate_staffing_ratio(100)
    assert result.recommended_ushers == 2   # 100/100 = 1, but min is 2
    assert result.recommended_registration_staff == 1


def test_calculate_staffing_ratio_boundary_capacity_300():
    result = calculate_staffing_ratio(300)
    assert result.recommended_ushers == 3   # 300/100
    assert result.recommended_registration_staff == 1   # 300/300 = 1


# ---------------------------------------------------------------------------
# Rounding behavior
# ---------------------------------------------------------------------------
def test_calculate_staffing_ratio_rounds_up():
    """101 attendees → 2 ushers (ceiling of 1.01)."""
    result = calculate_staffing_ratio(101)
    assert result.recommended_ushers == 2


def test_calculate_staffing_ratio_rounds_up_large():
    """301 attendees → 4 ushers (ceiling of 3.01)."""
    result = calculate_staffing_ratio(301)
    assert result.recommended_ushers == 4


# ---------------------------------------------------------------------------
# Input validation
# ---------------------------------------------------------------------------
def test_calculate_staffing_ratio_rejects_zero():
    with pytest.raises(ValueError, match="greater than 0"):
        calculate_staffing_ratio(0)


def test_calculate_staffing_ratio_rejects_negative():
    with pytest.raises(ValueError, match="greater than 0"):
        calculate_staffing_ratio(-50)


# ---------------------------------------------------------------------------
# Reasoning string is populated
# ---------------------------------------------------------------------------
def test_calculate_staffing_ratio_includes_reasoning():
    result = calculate_staffing_ratio(500)
    assert "Capacity 500" in result.reasoning
    assert "ushers" in result.reasoning
    assert "registration staff" in result.reasoning