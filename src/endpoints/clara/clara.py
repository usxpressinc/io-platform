import logging
from typing import Annotated
from urllib.parse import urljoin

import requests
from fastapi import HTTPException, Query, Security, status
from pydantic import BaseModel

from src.endpoints.router import router
from src.helpers.auth import authenticate_token
from src.settings import Settings

settings = Settings()
logger = logging.getLogger(__name__)


class CarrierResponse(BaseModel):
    valid: bool = False
    errors: list[str] = []


@router.get(
    "/clara/carriers",
    tags=["highway"],
    summary="Check whether a carrier is valid using Highway API",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=CarrierResponse,
)
def get_carrier_validity(
    authenticated: bool = Security(authenticate_token, scopes=["example"]),
    dotNumber: Annotated[str | None, Query()] = None,
    mcNumber: Annotated[str | None, Query()] = None,
) -> CarrierResponse:
    """
    ## Check whether a carrier is valid using Highway API
    """
    headers = {
        "Authorization": f"Bearer {settings.Lea_HighwayApiKey}",
    }

    if dotNumber is not None:
        path = (
            f"/core/connect/external_api/v1/carriers/{dotNumber}/by_dot_number"
        )
    if mcNumber is not None:
        path = f"/core/connect/external_api/v1/carriers/{mcNumber}"

    if path is None:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=CarrierResponse(valid=False).model_dump(),
        )
    c_response = requests.get(
        url=urljoin(settings.Lea_HighwayUrl, path), headers=headers
    )
    carrier_json = c_response.json()
    if not c_response.ok:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=CarrierResponse(valid=False).model_dump(),
        )
    print(carrier_json)
    return CarrierResponse(valid=False)
