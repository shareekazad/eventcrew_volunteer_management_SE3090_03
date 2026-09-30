"""
Smoke test for all tools. Run from ai-service/ with:
    python -m tests.test_tools_smoke

Requires the ASP.NET Core API running on port 5100.
"""

import asyncio
from app.tools.event_tools import get_event
from app.tools.venue_tools import get_venue
from app.tools.scheduling_tools import calculate_staffing_ratio

# Replace with real IDs from your DB
EVENT_ID = "f19b35ea-0355-4b95-acac-b58c820dbc3c"
VENUE_ID = "1bbac072-5297-49da-8461-67daf7a25a61"


async def main():
    print("=" * 60)
    print("TOOL 1: get_event")
    print("=" * 60)
    event = await get_event(EVENT_ID)
    if event is None:
        print("Event not found.")
        return
    print(f"  Title:  {event.title}")
    print(f"  Status: {event.status}")
    print(f"  Roles:  {len(event.role_requirements)}")

    print()
    print("=" * 60)
    print("TOOL 2: get_venue")
    print("=" * 60)
    venue = await get_venue(VENUE_ID)
    if venue is None:
        print("Venue not found.")
        return
    print(f"  Name:     {venue.name}")
    print(f"  City:     {venue.city}")
    print(f"  Capacity: {venue.capacity}")

    print()
    print("=" * 60)
    print("TOOL 3: calculate_staffing_ratio (pure math)")
    print("=" * 60)
    result = calculate_staffing_ratio(venue.capacity)
    print(f"  Capacity:   {result.venue_capacity}")
    print(f"  Ushers:     {result.recommended_ushers}")
    print(f"  Reg staff:  {result.recommended_registration_staff}")
    print(f"  Total staff: {result.total_staff}")
    print(f"  Reasoning:  {result.reasoning}")


if __name__ == "__main__":
    asyncio.run(main())