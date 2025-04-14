from fastapi import APIRouter, Security, status

from src.helpers.auth import authenticate_token

from .carrier_vetting import carrier_vetting_models, carrier_vetting_service

router = APIRouter(prefix="/api/clara", include_in_schema=True, tags=["clara"])


@router.post(
    "/carriers/valid",
    summary="Check whether a carrier is valid using Highway API",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=carrier_vetting_models.CarrierValidityResponse,
)
def get_carrier_validity(
    item: carrier_vetting_models.CarrierValidityRequest,
    authenticated: bool = Security(authenticate_token, scopes=["clara"]),
) -> carrier_vetting_models.CarrierValidityResponse:
    """
    ## Check whether a carrier is valid using Highway API
    """
    return carrier_vetting_service.get_carrier_validity(
        dotNumber=item.dotNumber,
        mcNumber=item.mcNumber,
        movementNumber=item.movementNumber,
    )
