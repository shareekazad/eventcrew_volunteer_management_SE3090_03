"""
Reusable HTTP client for calling the ASP.NET Core backend.

Design:
- Single shared client (connection pooling, reuse)
- Base URL from environment variable (so dev/prod differ without code changes)
- Explicit timeout (no hanging calls)
- Structured error handling, including timeouts and connection failures
"""

import os
import httpx
from typing import Any


class BackendError(Exception):
    """Raised when the backend call fails (timeout, connection refused, 5xx)."""


class BackendClient:
    """
    Thin wrapper around httpx.AsyncClient for calling the ASP.NET Core backend.
    """

    def __init__(self, base_url: str | None = None, timeout_seconds: float = 30.0):
        self.base_url = base_url or os.getenv(
            "BACKEND_BASE_URL", "http://localhost:5100"
        )
        self._timeout = timeout_seconds
        self._client = httpx.AsyncClient(
            base_url=self.base_url,
            timeout=timeout_seconds,
        )

    async def get(self, path: str) -> dict[str, Any] | None:
        """
        GET a resource. Returns the parsed JSON dict, or None if 404.

        Raises BackendError on timeout, connection failure, or 5xx.
        """
        try:
            response = await self._client.get(path)
        except httpx.TimeoutException as exc:
            raise BackendError(
                f"Backend timed out after {self._timeout}s while calling {path}. "
                f"Is the ASP.NET Core API running on {self.base_url}?"
            ) from exc
        except httpx.ConnectError as exc:
            raise BackendError(
                f"Could not connect to backend at {self.base_url} while calling {path}. "
                f"Is the ASP.NET Core API running?"
            ) from exc

        if response.status_code == 404:
            return None
        if response.status_code >= 500:
            raise BackendError(f"Backend error {response.status_code} calling {path}: {response.text}")
        response.raise_for_status()
        return response.json()

    async def close(self) -> None:
        await self._client.aclose()

    async def __aenter__(self) -> "BackendClient":
        return self

    async def __aexit__(self, *_) -> None:
        await self.close()