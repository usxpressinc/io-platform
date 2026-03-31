from fastapi import APIRouter, HTTPException, Response, Security, status

from src.helpers.auth import authenticate_token
from src.models.response import LLMResponse

from .carrier_vetting import carrier_vetting_models, carrier_vetting_service

router = APIRouter(prefix="/api/clara", include_in_schema=True, tags=["clara"])


@router.post(
    "/carriers/valid",
    summary="Check whether a carrier is valid using Highway API",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_202_ACCEPTED,
    response_model=LLMResponse[carrier_vetting_models.CarrierValidityResponse],
)
async def get_carrier_validity(
    item: carrier_vetting_models.CarrierValidityRequest,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["clara"]),
) -> LLMResponse[carrier_vetting_models.CarrierValidityResponse]:
    """
    ## Check whether a carrier is valid using Highway API
    """
    try:
        result = await carrier_vetting_service.get_carrier_validity(
            dotNumber=item.dotNumber,
            mcNumber=item.mcNumber,
            brokerage_order_id=item.brokerageOrderId,
        )
        response.status_code = status.HTTP_200_OK
        return LLMResponse[carrier_vetting_models.CarrierValidityResponse](
            data=result, response_schema=carrier_vetting_models.schema
        )
    except HTTPException as e:
        response.status_code = status.HTTP_200_OK
        # If detail is already a model instance, convert to dict to avoid double nesting
        detail_data = (
            e.detail.model_dump()
            if isinstance(
                e.detail, carrier_vetting_models.CarrierValidityResponse
            )
            else e.detail
        )
        return LLMResponse[carrier_vetting_models.CarrierValidityResponse](
            data=detail_data, response_schema=carrier_vetting_models.schema
        )
