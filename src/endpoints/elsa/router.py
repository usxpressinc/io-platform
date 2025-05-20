import typing

from fastapi import (
    APIRouter,
    HTTPException,
    Request,
    Response,
    Security,
    status,
)

from src.helpers.auth import authenticate_token
from src.models.response import LLMResponse

from .price import pricing_service

router = APIRouter(prefix="/api/elsa", include_in_schema=True, tags=["elsa"])


@router.post(
    "/price/lookup",
    summary="Get Recommended Price for the Load",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=LLMResponse[dict],
)
async def lookup_price(
    request: Request,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["elsa"]),
) -> LLMResponse[dict]:
    try:
        body = await request.json()
        result = pricing_service.lookup_price(body=body)
    except HTTPException as e:
        response.status_code = e.status_code
        result = e.detail
    return LLMResponse[dict](
        data=typing.cast(dict, result), response_schema=dict()
    )
