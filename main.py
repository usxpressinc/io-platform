import logging
import os

import uvicorn

from src.settings import Settings

logger = logging.getLogger(__name__)


if __name__ == "__main__":
    settings = Settings()
    os.environ["OTEL_EXPORTER_OTLP_ENDPOINT"] = settings.OtlpExporterEndpoint
    logger.info("Starting Uvicorn")
    uvicorn.run(
        app="src.app:app",
        host=settings.Host,
        port=settings.Port,
        use_colors=True,
    )
