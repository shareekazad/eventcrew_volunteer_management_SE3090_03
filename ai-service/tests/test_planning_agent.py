"""
Smoke test for PlanningAgent. Run from ai-service/ with:
    python -m tests.test_planning_agent

Requires the ASP.NET Core API running on port 5100.
"""

import asyncio
import json
from app.agents.planning_agent import PlanningAgent

# Replace with your real event ID if needed
EVENT_ID = "f19b35ea-0355-4b95-acac-b58c820dbc3c"


async def main():
    agent = PlanningAgent()

    print("=" * 70)
    print("PlanningAgent — running against event:", EVENT_ID)
    print("=" * 70)

    try:
        result = await agent.plan(EVENT_ID)
    except ValueError as e:
        print(f"AGENT FAILED: {e}")
        return

    print()
    print("OBJECTIVE:", result.objective)
    print("NEXT AGENT:", result.next_agent)
    print("STATUS:", result.status)

    print()
    print("-" * 70)
    print("PLAN STEPS:")
    print("-" * 70)
    for step in result.steps:
        tool_info = f" (tool: {step.tool})" if step.tool else " (no tool)"
        print(f"  {step.step_number}. [{step.agent}] {step.action}{tool_info}")

    print()
    print("-" * 70)
    print("REASONING:")
    print("-" * 70)
    print(f"  {result.reasoning}")

    print()
    print("-" * 70)
    print("TOOL CALL LOG (audit trail):")
    print("-" * 70)
    for log in result.tool_calls:
        print(f"  → {log.tool_name}")
        print(f"      input:    {log.input_params}")
        print(f"      output:   {log.output_summary}")
        print(f"      duration: {log.duration_ms} ms")
        print(f"      at:       {log.called_at}")

    print()
    print("=" * 70)
    print("FULL JSON OUTPUT:")
    print("=" * 70)
    print(json.dumps(result.model_dump(), indent=2, default=str))


if __name__ == "__main__":
    asyncio.run(main())