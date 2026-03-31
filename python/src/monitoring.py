import logging

from fastapi import FastAPI
from opentelemetry import _logs, metrics, trace
from opentelemetry.exporter.otlp.proto.grpc._log_exporter import OTLPLogExporter
from opentelemetry.exporter.otlp.proto.grpc.metric_exporter import (
    OTLPMetricExporter,
)
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import (
    OTLPSpanExporter,
)
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from opentelemetry.instrumentation.httpx import HTTPXClientInstrumentor
from opentelemetry.instrumentation.logging import LoggingInstrumentor
from opentelemetry.instrumentation.logging.constants import (
    DEFAULT_LOGGING_FORMAT,
)
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.metrics import MeterProvider
from opentelemetry.sdk.metrics.export import PeriodicExportingMetricReader
from opentelemetry.sdk.resources import (
    OsResourceDetector,
    OTELResourceDetector,
    ProcessResourceDetector,
    Resource,
    _HostResourceDetector,
    get_aggregated_resources,
)
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.semconv.attributes import service_attributes
from pythonjsonlogger.json import JsonFormatter

from src.middleware.logging_middleware import RequestResponseLoggingMiddleware
from src.settings import Settings

handler: LoggingHandler | None = None


class OtelProviders:
    def __init__(
        self,
        tracer_provider: TracerProvider,
        logger_provider: LoggerProvider,
        metrics_provider: MeterProvider,
    ):
        self.tracer_provider = tracer_provider
        self.logger_provider = logger_provider
        self.metrics_provider = metrics_provider


def set_tracer(resource: Resource) -> TracerProvider:
    """Set Tracer Configuration

    Args:
        resource (Resource): Otel Resource Object

    Returns:
        TracerProvider: Tracer Provider
    """
    tracer_provider = TracerProvider(resource=resource)
    trace.set_tracer_provider(tracer_provider)
    tracer_provider.add_span_processor(
        BatchSpanProcessor(
            span_exporter=OTLPSpanExporter(),
            max_export_batch_size=64,
            max_queue_size=256,
        )
    )
    return tracer_provider


def set_metrics(resource: Resource) -> MeterProvider:
    """Set Meter Configuration

    Args:
        resource (Resource): Otel Resource Object

    Returns:
        MeterProvider: Meter Provider
    """
    meter_provider = MeterProvider(
        resource=resource,
        metric_readers=[PeriodicExportingMetricReader(OTLPMetricExporter())],
    )
    metrics.set_meter_provider(meter_provider)
    return meter_provider


def set_logger(resource: Resource) -> LoggerProvider:
    """Set Logger Configuration

    Args:
        resource (Resource): Otel Resource Object

    Returns:
        LoggerProvider: Logger Provider
    """
    settings = Settings.model_validate({})
    LoggingInstrumentor().instrument(set_logging_format=False)
    # Create and set the logger provider
    logger_provider = LoggerProvider(resource=resource)
    _logs.set_logger_provider(logger_provider)
    # Create the OTLP log exporter that sends logs to configured destination
    logger_provider.add_log_record_processor(
        BatchLogRecordProcessor(
            exporter=OTLPLogExporter(),
            max_export_batch_size=64,
            max_queue_size=256,
        )
    )
    # Attach OTLP handler to root logger
    handler = LoggingHandler(logger_provider=logger_provider)
    handler.setFormatter(JsonFormatter())

    logger = logging.getLogger()
    logging.basicConfig(
        force=True, format=DEFAULT_LOGGING_FORMAT, level=settings.LogLevel
    )
    logger.addHandler(handler)
    logging.getLogger("uvicorn").addHandler(handler)
    return logger_provider


def setting_otlp(app: FastAPI) -> OtelProviders:
    """Set OTLP Providers

    Args:
        app (FastAPI): Fastapi APP

    Returns:
        LoggerProvider: Returns LoggerProvider
    """
    global handler
    settings = Settings.model_validate({})
    resource = get_aggregated_resources(
        [
            _HostResourceDetector(),
            OsResourceDetector(),
            OTELResourceDetector(),
            ProcessResourceDetector(),
        ],
        Resource.create(
            {
                service_attributes.SERVICE_NAME: f"{settings.Environment}/{settings.Group}/{settings.Project}",
                service_attributes.SERVICE_VERSION: settings.Revision,
                "project": settings.Project,
                "group": settings.Group,
                "environment": settings.Environment,
            }
        ),
    )

    tracer_provider = set_tracer(resource=resource)
    logger_provider = set_logger(resource=resource)
    metrics_provider = set_metrics(resource=resource)

    HTTPXClientInstrumentor().instrument()
    FastAPIInstrumentor.instrument_app(
        app=app,
        tracer_provider=tracer_provider,
        excluded_urls="/swagger.*,/openapi.*",
        meter_provider=metrics_provider,
    )

    app.add_middleware(RequestResponseLoggingMiddleware)

    return OtelProviders(
        tracer_provider=tracer_provider,
        metrics_provider=metrics_provider,
        logger_provider=logger_provider,
    )
