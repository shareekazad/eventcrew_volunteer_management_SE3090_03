# ────────────────────────────────────────────────────────────────────────────
# EventCrew Python AI Service — Production Dockerfile
# Uses the full Debian Python image (build tools already included)
# ────────────────────────────────────────────────────────────────────────────

FROM python:3.11 AS runtime

WORKDIR /app

# Copy requirements first for better layer caching
COPY requirements.txt .

# Install Python deps (no apt-get needed — build tools are baked into the image)
RUN pip install --no-cache-dir --upgrade pip && \
    pip install --no-cache-dir -r requirements.txt

# Copy the application
COPY . .

# Railway assigns $PORT dynamically — default to 8000
ENV PORT=8000
ENV PYTHONUNBUFFERED=1

# Expose port
EXPOSE 8000

# Healthcheck endpoint
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:${PORT}/docs')" || exit 1

# Run uvicorn
CMD ["sh", "-c", "uvicorn app.main:app --host 0.0.0.0 --port ${PORT}"]