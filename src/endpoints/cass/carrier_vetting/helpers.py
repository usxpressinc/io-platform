import json
import logging
from urllib.parse import urlencode, urljoin

import httpx
from fastapi import HTTPException, status

from src.prompts import ClaraPrompts
from src.settings import Settings

from . import models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)
prompts = ClaraPrompts()


async def get_highway_details(
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
        path = f"/core/connect/external_api/v1/carriers/MC/{mcNumber}/by_identifier"

    if not path:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_dot_mcNumber",
                        description=prompts.invalid_dot_mcNumber,
                    )
                ],
                failedBy=["highway"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )
    async with httpx.AsyncClient(verify=False) as client:
        c_response = await client.get(
            url=urljoin(settings.Clara_HighwayUrl, path), headers=headers
        )

    if not c_response.text:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_carrier",
                        description=prompts.use_transfer_to_carrier_sales_rep,
                    )
                ],
                failedBy=["highway"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )
    carrier_text = c_response.text
    if c_response.is_error:
        logger.error("Error thrown %s", repr(carrier_text))
        if c_response.status_code >= 500:
            raise HTTPException(
                status_code=status.HTTP_502_BAD_GATEWAY,
                detail=models.CarrierValidityResponse(
                    isValid="false",
                    errors=[
                        models.CarrierValidityError(
                            code="highway_down",
                            description=prompts.use_transfer_to_carrier_sales_rep,
                        )
                    ],
                    failedBy=["highway"],
                    statusCode=status.HTTP_502_BAD_GATEWAY,
                ),
            )
        else:
            raise HTTPException(
                status_code=c_response.status_code,
                detail=models.CarrierValidityResponse(
                    isValid="false",
                    errors=[
                        models.CarrierValidityError(
                            code="highway_issue",
                            description=prompts.use_transfer_to_carrier_sales_rep,
                        )
                    ],
                    failedBy=["highway", carrier_text],
                    statusCode=c_response.status_code,
                ),
            )
    return json.loads(carrier_text)


async def get_mcleod_carrier_details(
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
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_dot_mc_number",
                        description=prompts.invalid_dot_mcNumber,
                    )
                ],
                failedBy=["Mcleod"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )
    search = ""
    if dotNumber:
        logger.info("dotNumber is not None")
        search = f"drsPayee.dot_number={dotNumber}"
    elif mcNumber:
        logger.info("mcNumber is not None")
        search = f"drsPayee.icc_number={mcNumber}"

    async with httpx.AsyncClient() as client:
        c_response = await client.get(
            url=urljoin(
                settings.Clara_McleodUrl, f"/ws/api/carriers/search?{search}"
            ),
            headers=headers,
        )
    if c_response.is_error:
        logger.error("Error thrown %s", c_response.text)
        if c_response.status_code >= 500:
            raise HTTPException(
                status_code=status.HTTP_502_BAD_GATEWAY,
                detail=models.CarrierValidityResponse(
                    isValid="false",
                    errors=[
                        models.CarrierValidityError(
                            code="invalid_dot_mc_number",
                            description=prompts.use_transfer_to_carrier_sales_rep,
                        )
                    ],
                    failedBy=["Mcleod", c_response.text],
                    statusCode=status.HTTP_502_BAD_GATEWAY,
                ),
            )
        else:
            raise HTTPException(
                status_code=c_response.status_code,
                detail=models.CarrierValidityResponse(
                    isValid="false",
                    errors=[
                        models.CarrierValidityError(
                            code="invalid_dot_mc_number",
                            description=prompts.use_transfer_to_carrier_sales_rep,
                        )
                    ],
                    failedBy=["Mcleod", c_response.text],
                    statusCode=c_response.status_code,
                ),
            )
    if c_response.text == "" or c_response.json is None:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_carrier",
                        description=prompts.invalid_dot_mcNumber,
                    )
                ],
                failedBy=["Mcleod"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )
    return c_response.json()


async def get_mcleod_order(order_id: str | None):
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    if not order_id:
        logger.error("order_id is empty")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_order",
                        description=prompts.use_tentative_pass,
                    )
                ],
                failedBy=["Mcleod", "brokerageOrderId_empty"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "application/json",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    try:
        int(order_id)
    except ValueError as e:
        logger.error(repr(e))
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_order",
                        description=prompts.invalid_order,
                    )
                ],
                failedBy=["Mcleod", "brokerageOrderId_invalid"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )

    async with httpx.AsyncClient() as client:
        c_response = await client.get(
            url=urljoin(settings.Clara_McleodUrl, f"/ws/api/orders/{order_id}"),
            headers=headers,
        )
    if not c_response.text:
        logger.error(
            "Mcleod Get Order call failed with %s - %s",
            c_response.status_code,
            c_response.text,
        )
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="invalid_order",
                        description=prompts.invalid_order,
                    )
                ],
                failedBy=["Mcleod", "brokerageOrderId_invalid"],
                statusCode=status.HTTP_400_BAD_REQUEST,
            ),
        )

    if c_response.is_error:
        logger.error("Error thrown %s", c_response.text)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="mcleod_down",
                        description=prompts.use_transfer_to_carrier_sales_rep,
                    )
                ],
                failedBy=["Mcleod", c_response.text],
                statusCode=c_response.status_code,
            ),
        )
    result = c_response.json()
    return result


async def check_mcleod_carrier_qualification(
    carrier_id: str, movement: str
) -> bool:
    """
    ## Check whether a carrier is valid using Mcleod API
    """
    logger.debug("Movement Id is %s", movement)
    headers = {
        "Authorization": settings.Clara_McleodAuth,
        "Accept": "text/plain",
        "X-com.mcleodsoftware.CompanyID": settings.Clara_McleodCompany,
    }
    url = urljoin(
        settings.Clara_McleodUrl, "/ws/api/carriers/checkQualification"
    )
    params = {"carrier": carrier_id, "movement": movement}
    query_string = urlencode(params, doseq=True)
    url = f"{url}?{query_string}"

    async with httpx.AsyncClient() as client:
        c_response = await client.get(url=url, headers=headers)
    result = c_response.text
    logger.info("Mcleod Carrier Validity is %s", result)
    if c_response.is_error:
        logger.error("Error thrown %s", repr(result))
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="mcleod_down",
                        description=prompts.use_transfer_to_carrier_sales_rep,
                    )
                ],
                failedBy=["Mcleod", c_response.text],
                statusCode=c_response.status_code,
            ),
        )
    return result.lower() == "true"
