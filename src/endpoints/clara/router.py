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
def get_carrier_validity(
    item: carrier_vetting_models.CarrierValidityRequest,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["clara"]),
) -> carrier_vetting_models.CarrierValidityResponse:
    """
    ## Check whether a carrier is valid using Highway API
    """
    try:
        result = carrier_vetting_service.get_carrier_validity(
            dotNumber=item.dotNumber,
            mcNumber=item.mcNumber,
            brokerage_order_id=item.brokerageOrderId,
        )
        if result.error:
            result.error.description = (
                " ".join(result.error.description.split())
                .replace("\n", " ")
                .replace("\r", "")
            )
        response.status_code = status.HTTP_202_ACCEPTED
        return LLMResponse[carrier_vetting_models.CarrierValidityResponse](
            data=result, schema=carrier_vetting_models.schema
        )
    except HTTPException as e:
        response.status_code = status.HTTP_202_ACCEPTED
        return e.detail
