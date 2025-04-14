import logging

from src.monitoring import OtelProviders

logger = logging.getLogger(__name__)
otel_providers: OtelProviders = None


async def shutdown_event():
    """Runs during app shutdown"""
    global otel_providers
    logging.info("Cleaning up logging")
    if otel_providers.logger_provider is not None:
        otel_providers.logger_provider.shutdown()
    logging.info("Cleaning up tracing")
    if otel_providers.tracer_provider is not None:
        otel_providers.tracer_provider.shutdown()
    logging.info("Cleaning up metrics")
    if otel_providers.metrics_provider is not None:
        otel_providers.metrics_provider.shutdown()
