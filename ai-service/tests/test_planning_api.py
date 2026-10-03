"""FastAPI boundary tests; the planning graph remains fully local and deterministic."""

from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def valid_payload() -> dict:
    return {
        "event_id": "11111111-1111-1111-1111-111111111111",
        "event": {
            "id": "11111111-1111-1111-1111-111111111111",
            "venue_id": "22222222-2222-2222-2222-222222222222",
            "title": "Boundary Test Event",
            "description": None,
            "category": "Community",
            "start_date": "2026-11-01T09:00:00Z",
            "end_date": "2026-11-01T17:00:00Z",
            "status": "Draft",
            "role_requirements": [
                {
                    "id": "33333333-3333-3333-3333-333333333333",
                    "role_name": "Guide",
                    "required_headcount": 4,
                    "min_experience_level": "Beginner",
                }
            ],
        },
        "venue": {
            "id": "22222222-2222-2222-2222-222222222222",
            "name": "Boundary Test Hall",
            "address": "1 Main Street",
            "city": "Colombo",
            "latitude": None,
            "longitude": None,
            "capacity": 250,
        },
    }


def test_plan_endpoint_returns_structured_graph_output_without_credentials():
    response = client.post("/agent/plan", json=valid_payload())

    assert response.status_code == 200
    plan = response.json()
    assert plan["status"] == "planned"
    assert [call["tool_name"] for call in plan["tool_calls"]] == [
        "get_event",
        "get_venue",
        "calculate_staffing_ratio",
    ]
    assert plan["staffing_recommendations"][0]["required_headcount"] == 4


def test_plan_endpoint_rejects_event_id_mismatch():
    payload = valid_payload()
    payload["event_id"] = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"

    response = client.post("/agent/plan", json=payload)

    assert response.status_code == 400
    assert "does not match" in response.json()["detail"]


def test_plan_endpoint_returns_validation_error_when_venue_is_missing():
    payload = valid_payload()
    payload["event"]["venue_id"] = None
    payload["venue"] = None

    response = client.post("/agent/plan", json=payload)

    assert response.status_code == 400
    assert "no venue" in response.json()["detail"]
