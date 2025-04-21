import logging
from urllib.parse import urljoin

import requests
from fastapi import HTTPException, status
from requests.models import PreparedRequest

from src.models.response import LLMResponse
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

    path = ""
    if dotNumber:
        logger.info("dotNumber is not None")
        path = (
            f"/core/connect/external_api/v1/carriers/{dotNumber}/by_dot_number"
        )
    if mcNumber:
        logger.info("mcNumber is not None")
        path = f"/core/connect/external_api/v1/carriers//MC/{mcNumber}/by_identifier"

    if not path:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_dot_mcNumber",
                        description="dotNumber && mcNumber is empty",
                    ),
                    failedBy=["highway"],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    c_response = requests.get(
        url=urljoin(settings.Clara_HighwayUrl, path), headers=headers
    )
    if not c_response.text:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_carrier", description="carrier is invalid"
                    ),
                    failedBy=["highway"],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    carrier_json = c_response.json()
    if not c_response.ok:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="highway_down", description="Highway API is down"
                    ),
                    failedBy=["highway"],
                    statusCode=status.HTTP_502_BAD_GATEWAY,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    return carrier_json


def get_mcleod_carrier_details(
    dotNumber: int | None = None,
    mcNumber: int | None = None,
):
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "application/json",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    if not (dotNumber or mcNumber):
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_dot_mc_number",
                        description="dotNumber or mcNumber should not be empty from highway API",
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
                schema=models.schema,
            ).model_dump(),
        )
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
    carrier_json = c_response.json()
    if not c_response.ok:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_dot_mc_number",
                        description="Mcleod is down",
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_502_BAD_GATEWAY,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    if carrier_json is None:
        logger.error("Error thrown %s", repr(carrier_json))
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_carrier",
                        description="Mcleod couldn't find carrier",
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    return carrier_json


def get_mcleod_order(order_id: str):
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    if not order_id:
        logger.error("order_id is empty")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_order",
                        description="brokerageOrderId should not be empty",
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "application/json",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    c_response = requests.get(
        url=urljoin(settings.Clara_McleodUrl, f"/ws/api/orders/{order_id}"),
        headers=headers,
    )
    if not c_response.text:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="invalid_order",
                        description="brokerageOrderId is invalid",
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
                schema=models.schema,
            ).model_dump(),
        )

    if not c_response.ok:
        logger.error("Error thrown %s", c_response.text)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="mcleod_down", description="Mcleod id down"
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_502_BAD_GATEWAY,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    result = c_response.json()
    logger.info("Mcleod Order is %s", result)
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
            detail=LLMResponse[models.CarrierValidityResponse](
                data=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="mcleod_down", description="Mcleod is down"
                    ),
                    failedBy=["Mcleod"],
                    statusCode=status.HTTP_502_BAD_GATEWAY,
                ),
                schema=models.schema,
            ).model_dump(),
        )
    return result.lower() == "true"
