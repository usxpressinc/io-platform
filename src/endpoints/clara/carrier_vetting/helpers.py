import logging
from urllib.parse import urljoin

import requests
from fastapi import HTTPException, status
from requests.models import PreparedRequest

from src.settings import Settings

from . import models

settings = Settings()
logger = logging.getLogger(__name__)


def get_highway_details(
    dotNumber: str | None = None,
    mcNumber: str | None = None,
):
    """
    ## Check whether a carrier is valid using Highway API
    """
    headers = {
        "Authorization": f"Bearer {settings.Clara_HighwayApiKey}",
    }

    logger.info("dotNumber %s mcNumber %s", dotNumber, mcNumber)

    if dotNumber:
        logger.info("dotNumber is not None")
        path = (
            f"/core/connect/external_api/v1/carriers/{dotNumber}/by_dot_number"
        )
    if mcNumber:
        logger.info("mcNumber is not None")
        path = f"/core/connect/external_api/v1/carriers//MC/{mcNumber}/by_identifier"

    if path is None:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(valid=False).model_dump(),
        )
    c_response = requests.get(
        url=urljoin(settings.Clara_HighwayUrl, path), headers=headers
    )
    carrier_json = c_response.json()
    # logger.info("Carrier Json is %s", carrier_json)
    if not c_response.ok:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=models.CarrierValidityResponse(valid=False).model_dump(),
        )
    return carrier_json


def get_mcleod_carrier_details(
    dotNumber: str | None = None,
    mcNumber: str | None = None,
):
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "application/json",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    if dotNumber:
        logger.info("dotNumber is not None")
        search = f"drsPayee.dot_number={dotNumber}"
    if mcNumber:
        logger.info("mcNumber is not None")
        search = f"drsPayee.icc_number={mcNumber}"
    c_response = requests.get(
        url=urljoin(
            settings.Clara_McleodUrl, f"/ws/api/carriers/search?{search}"
        ),
        headers=headers,
    )
    logger.info("Carrier Json is %s", c_response.text)
    carrier_json = c_response.json()
    if not c_response.ok:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                valid=False, errors=["Mcleod id down"]
            ).model_dump(),
        )
    if carrier_json is None:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                valid=False, errors=["Mcleod couldn't find carrier"]
            ).model_dump(),
        )
    return carrier_json


def get_mcleod_order(order_id: str):
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "application/json",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    c_response = requests.get(
        url=urljoin(settings.Clara_McleodUrl, f"/ws/api/orders/{order_id}"),
        headers=headers,
    )
    result = c_response.json()
    logger.info("Mcleod Order is %s", result)
    if not c_response.ok:
        logger.error("Error thrown %s", repr(result))
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                valid=False, errors=["Mcleod id down"]
            ).model_dump(),
        )
    return result


def check_mcleod_carrier_qualification(carrier_id: str, movement: str) -> bool:
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "text/plain",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    req = PreparedRequest()
    req.prepare_url(
        url=urljoin(
            settings.Clara_McleodUrl, "/ws/api/carriers/checkQualification"
        ),
        params={"carrier": carrier_id, "movement": movement},
    )
    c_response = requests.get(url=req.url, headers=headers)
    result = c_response.text
    logger.info("Mcleod Carrier Validity is %s", result)
    if not c_response.ok:
        logger.error("Error thrown %s", repr(result))
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                valid=False, errors=["Mcleod id down"]
            ).model_dump(),
        )
    return result.lower() == "true"
