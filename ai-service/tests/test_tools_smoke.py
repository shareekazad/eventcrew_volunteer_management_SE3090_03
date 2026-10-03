"""Non-network smoke test for the deterministic scheduling tool."""
import pytest
from app.tools.scheduling_tools import calculate_staffing_ratio


def test_staffing_tool_returns_a_deterministic_recommendation():
    result = calculate_staffing_ratio(500)
    assert result.total_staff == 7
    assert "Capacity 500" in result.reasoning