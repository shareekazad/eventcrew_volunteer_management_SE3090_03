"""
Manual smoke test — run from repo root with:
    python -m ai_service.tests.test_get_event

Or from ai-service/ folder with:
    python tests/test_get_event.py
"""

import asyncio
from app.tools.event_tools import get_event

# Use the real event ID you created in Swagger earlier
EVENT_ID = "f19b35ea-0355-4b95-acac-b58c820dbc3c"


async def main():
    event = await get_event(EVENT_ID)
    if event is None:
        print("Event not found.")
        return
    print(f"Title: {event.title}")
    print(f"Status: {event.status}")
    print(f"Roles: {len(event.role_requirements)}")
    for r in event.role_requirements:
        print(f"  - {r.role_name} x {r.required_headcount} ({r.min_experience_level})")


if __name__ == "__main__":
    asyncio.run(main())