from fastapi import APIRouter

router = APIRouter(prefix="/api", include_in_schema=True)
