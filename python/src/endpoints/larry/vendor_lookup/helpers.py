import logging
from urllib.parse import urlencode

import httpx
from fastapi import HTTPException
from httpx import HTTPStatusError
from pydantic import TypeAdapter

from src.settings import Settings

from . import models

logger = logging.getLogger(__name__)
settings = Settings.model_validate({})
VendorServiceMap: dict[int, list[str]] = dict()


async def vendors_services() -> list[models.VendorService]:
    url = "{}{}".format(
        settings.Larry_xpm_api, "/api/xra/roadsideVendorServices"
    )
    logger.info(url)
    params = {
        "url": url,
    }
    query_string = urlencode(params, doseq=True)
    proxy = f"{settings.OnPrem_proxy}?{query_string}"

    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(proxy)
        response.raise_for_status()
    except HTTPStatusError as e:
        logger.error(e.response.text)
        raise HTTPException(
            status_code=e.response.status_code, detail={"error": str(e)}
        )
    adapter = TypeAdapter(list[models.VendorService])
    return adapter.validate_python(response.json())


async def vendors_service_codes() -> list[models.VendorServiceCode]:
    url = "{}{}".format(
        settings.Larry_xpm_api, "/api/xra/roadsideVendorServiceCodes"
    )
    logger.info(url)
    params = {
        "url": url,
    }
    query_string = urlencode(params, doseq=True)
    proxy = f"{settings.OnPrem_proxy}?{query_string}"

    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(proxy)
        response.raise_for_status()
    except HTTPStatusError as e:
        logger.error(e.response.text)
        raise HTTPException(
            status_code=e.response.status_code, detail={"error": str(e)}
        )
    adapter = TypeAdapter(list[models.VendorServiceCode])
    return adapter.validate_python(response.json())


async def vendors_by_location(body: dict) -> list[models.Vendor]:
    logger.info(body)
    url = "{}{}".format(settings.Larry_xpm_api, "/api/xra/vendorsbylocation")
    logger.info(url)
    params = {
        "url": url,
    }
    query_string = urlencode(params, doseq=True)
    proxy = f"{settings.OnPrem_proxy}?{query_string}"

    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(proxy, json=body)
        response.raise_for_status()
    except HTTPStatusError as e:
        logger.error(e.response.text)
        raise HTTPException(
            status_code=e.response.status_code, detail={"error": str(e)}
        )
    adapter = TypeAdapter(list[models.Vendor])
    return adapter.validate_python(response.json())
