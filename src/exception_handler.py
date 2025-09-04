import httpx
from fastapi import FastAPI, Request, status
from fastapi.responses import JSONResponse


def setup_exception_handlers(app: FastAPI):
    @app.exception_handler(httpx.ConnectError)
    async def connect_error_handler(request: Request, exc: httpx.ConnectError):
        url = str(exc.request.url) if exc.request else "Unknown URL"
        method = exc.request.method if exc.request else "Unknown Method"

        return JSONResponse(
            status_code=status.HTTP_502_BAD_GATEWAY,
            content={
                "detail": f"Connection failed for {method} {url}: {str(exc)}",
                "error_type": "connection_error",
                "url": url,
                "method": method,
            },
        )
