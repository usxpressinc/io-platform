FROM python:3.13-slim AS base

FROM base AS builder

ENV POETRY_HOME=/opt/poetry
ENV POETRY_VIRTUALENVS_IN_PROJECT=1
ENV POETRY_VIRTUALENVS_CREATE=1
ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

# Install poetry
RUN pip install "poetry>=2,<3"

WORKDIR /app

COPY pyproject.toml poetry.lock /app/

RUN poetry install --no-root && rm -rf $POETRY_CACHE_DIR

FROM base AS runtime

ENV VIRTUAL_ENV=/app/.venv
ENV PATH="/app/.venv/bin:$PATH"

WORKDIR /app

COPY --from=builder ${VIRTUAL_ENV} ${VIRTUAL_ENV}

COPY . /app

CMD ["python3", "/app/main.py"]
