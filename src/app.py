import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

import src.events as events
from src.endpoints.health import router as health_router
from src.endpoints.router import routers as endpoints_routers
from src.events import shutdown_event
from src.monitoring import setting_otlp
from src.settings import Settings

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield
    await shutdown_event()


def create_app() -> FastAPI:
    """Create FastAPI App

    Args:
        settings (Settings): App Settings

    Returns:
        FastAPI: FastAPI App
    """
    settings = Settings()
    app = FastAPI(
        title=settings.Project,
        description="Gateway for some QTops Calls",
        version=settings.Revision,
        docs_url="/swagger",
        lifespan=lifespan,
        contact={
            "name": "Vibin Daniel",
            "url": "https://usxpress.freshservice.com",
            "email": "vdaniel@usxpress.com",
        },
        redoc_url="/",
        swagger_ui_oauth2_redirect_url="/swagger/oauth2-redirect",
    )
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_methods=["*"],
        allow_headers=["*"],
    )

    events.otel_providers = setting_otlp(app=app)

    for r in endpoints_routers:
        app.include_router(r)
    app.include_router(health_router)
    return app


app = create_app()
