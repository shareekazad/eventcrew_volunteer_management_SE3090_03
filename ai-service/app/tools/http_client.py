"""
Reusable HTTP client for calling the ASP.NET Core backend.

Design:
- Single shared client (connection pooling, reuse)
- Base URL from environment variable (so dev/prod differ without code changes)
- Explicit timeout (no hanging calls)
- Structured error handling
"""

import os
import httpx
from typing import Any


class BackendClient:
    """
    Thin wrapper around httpx.AsyncClient for calling the ASP.NET Core backend.
    """

    def __init__(self, base_url: str | None = None, timeout_seconds: float = 10.0):
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
        Raises httpx.HTTPStatusError on other 4xx/5xx responses.
        """
        response = await self._client.get(path)
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return response.json()

    async def close(self) -> None:
        await self._client.aclose()

    async def __aenter__(self) -> "BackendClient":
        return self

    async def __aexit__(self, *_) -> None:
        await self.close()