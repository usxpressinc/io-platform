import json
import logging
import time
from typing import Any, Dict

from fastapi import Request
from starlette.background import BackgroundTask
from starlette.concurrency import iterate_in_threadpool
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.responses import JSONResponse, Response, StreamingResponse

logger = logging.getLogger(__name__)


class RequestResponseLoggingMiddleware(BaseHTTPMiddleware):
    def get_client_info(self, request: Request) -> Dict[str, Any]:
        """Safely extract client information from request"""
        client_info = {"ip": "unknown", "port": None}

        if request.client:
            client_info["ip"] = request.client.host
            client_info["port"] = request.client.port
        else:
            forwarded_for = request.headers.get("x-forwarded-for")
            if forwarded_for:
                client_info["ip"] = forwarded_for.split(",")[0].strip()
            elif request.headers.get("x-real-ip"):
                client_info["ip"] = request.headers.get("x-real-ip")

        return client_info

    def log_request_response(self, req_data: Dict[str, Any]) -> None:
        """Background task to log request/response data"""
        level = logging.INFO
        if (
            req_data.get("response_status", 200) >= 400
            and req_data.get("response_status", 200) < 500
        ):
            level = logging.ERROR
        logger.log(level, req_data)

    async def get_response_body(self, response: Response) -> str:
        """Safely extract response body with proper type checking"""
        if isinstance(response, StreamingResponse):
            # Handle StreamingResponse
            res_body = []
            async for chunk in response.body_iterator:
                res_body.append(chunk)

            response.body_iterator = iterate_in_threadpool(iter(res_body))
            return b"".join(res_body).decode() if res_body else ""

        elif isinstance(response, JSONResponse):
            # Handle JSONResponse - it has body attribute
            if hasattr(response, "body"):
                return (
                    response.body.decode()
                    if isinstance(response.body, bytes)
                    else str(response.body)
                )

        elif isinstance(response, Response):
            # Handle regular Response
            if hasattr(response, "body"):
                body = response.body
                if isinstance(body, bytes):
                    return body.decode()
                return str(body)

        return ""

    async def dispatch(self, request: Request, call_next):
        # Get client info safely
        client_info = self.get_client_info(request)

        # Get request body
        req_body = None
        if request.method in ["POST", "PUT", "PATCH"]:
            try:
                req_body = await request.json()
            except Exception:
                try:
                    body_bytes = await request.body()
                    req_body = body_bytes.decode() if body_bytes else None
                except Exception:
                    req_body = None

        start_time = time.perf_counter()
        response = await call_next(request)
        process_time = time.perf_counter() - start_time

        # Get response body safely
        res_body_str = await self.get_response_body(response)

        try:
            res_body_json = json.loads(res_body_str) if res_body_str else None
        except:  # noqa: E722
            res_body_json = res_body_str

        # Prepare log data
        log_data = {
            "timestamp": time.time(),
            "method": request.method,
            "url": str(request.url),
            "path": request.url.path,
            "query_params": dict(request.query_params),
            "client": client_info,
            "request_body": req_body,
            "response_status": response.status_code,
            "response_body": res_body_json,
            "response_type": type(response).__name__,
            "process_time_ms": round(process_time * 1000, 2),
        }

        # Log in background to avoid blocking response
        response.background = BackgroundTask(
            self.log_request_response, log_data
        )
        return response
