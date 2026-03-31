from fastapi import APIRouter, HTTPException, Response, Security, status

from src.helpers.auth import authenticate_token
from src.models.response import LLMResponse

from .vendor_lookup import vendor_models, vendor_service

router = APIRouter(prefix="/api/larry", include_in_schema=True, tags=["larry"])


@router.post(
    "/vendor/lookup",
    summary="Get list of vendors for a location",
    response_description="Return HTTP Status Code 200 (OK)",
    response_model_exclude_none=True,
    response_model_exclude_unset=True,
    response_model_exclude_defaults=True,
    status_code=status.HTTP_200_OK,
    response_model=LLMResponse[vendor_models.VendorLookupResponse],
)
async def lookup_vendors(
    item: vendor_models.VendorLookupRequest,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["larry"]),
) -> LLMResponse[vendor_models.VendorLookupResponse]:
    try:
        result = await vendor_service.lookup_vendors(item=item)
    except HTTPException as e:
        response.status_code = e.status_code
        result = e.detail
    return LLMResponse[vendor_models.VendorLookupResponse](
        data=result, response_schema=vendor_models.schema
    )
