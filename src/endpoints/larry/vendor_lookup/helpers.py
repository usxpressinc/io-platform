import logging
from urllib.parse import urljoin

import httpx
from fastapi import HTTPException
from httpx import HTTPStatusError
from httpx_kerberos import HTTPKerberosAuth, MutualAuthentication
from httpx_kerberos.exceptions import KerberosExchangeError

from src.settings import Settings

logger = logging.getLogger(__name__)
settings = Settings.model_validate({})


async def vendors_by_location(body: dict) -> list:
    ex = None
    logger.info(body)
    for u in settings.Larry_xpm_api.split(","):
        try:
            url = urljoin(u, "/api/xra/vendorsbylocation")

            try:
                async with httpx.AsyncClient() as client:
                    response = await client.post(
                        url,
                        json=body,
                        auth=HTTPKerberosAuth(
                            mutual_authentication=MutualAuthentication.OPTIONAL,
                            principal=settings.Larry_xpm_api_principal,
                        ),
                        timeout=2.0,
                    )
            except KerberosExchangeError as e:
                raise HTTPException(status_code=502, detail={"error": str(e)})
            try:
                response.raise_for_status()
            except HTTPStatusError as e:
                raise HTTPException(
                    status_code=e.response.status_code, detail={"error": str(e)}
                )
            return response.json()
        except (HTTPException, TimeoutError) as e:
            ex = e
    if ex is not None:
        raise ex
    return []
