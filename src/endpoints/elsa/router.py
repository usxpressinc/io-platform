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

from .price import pricing_service, response_model

router = APIRouter(prefix="/api/elsa", include_in_schema=True, tags=["elsa"])


@router.post(
    "/price/lookup",
    summary="Get Recommended Price for the Load",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model_exclude_none=True,
    response_model=LLMResponse[response_model.LookupPriceResponse],
)
async def lookup_price(
    request: Request,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["elsa"]),
) -> LLMResponse[response_model.LookupPriceResponse]:
    try:
        body: dict = await request.json()
        result = pricing_service.lookup_price(body=body)
    except HTTPException as e:
        response.status_code = e.status_code
        result = e.detail
    return LLMResponse[response_model.LookupPriceResponse](
        data=result, response_schema=response_model.schema
    )
