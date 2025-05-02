from fastapi import APIRouter, HTTPException, Response, Security, status

from src.helpers.auth import authenticate_token
from src.models.response import LLMResponse

from .price import request, response, pricing_service

router = APIRouter(prefix="/api/elsa", include_in_schema=True, tags=["elsa"])


@router.post(
    "/price/lookup",
    summary="Get Recommended Price for the Load",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=LLMResponse[response.Response],
)
def lookup_jobs(
    item: request.Request,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["lea"]),
) -> LLMResponse[response.Response]:
    try:
        result = pricing_service.lookup_price(item=item)
    except HTTPException as e:
        response.status_code = e.status_code
        result = e.detail
    return LLMResponse[response.Response](
        data=result, schema=jobs_models.schema
    )
