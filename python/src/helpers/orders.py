import logging

import httpx
from fastapi import HTTPException, status

from src.helpers import msal
from src.settings import Settings

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def search_order_by_number(number: int):
    url = f"{settings.Orders_Url}/v1/orders/search"
    logger.info(url)

    body = {
        "data": [
            {"number": {"eq": number}},
        ],
        "project": {"_id": "id", "stops.lineItems.weight": "weights"},
    }

    token = await msal.get_access_token(
        client_id=settings.Auth_ClientId,
        client_secret=settings.Auth_ClientSecret,
        tenant=settings.Auth_TenantId,
        scopes=[settings.Orders_Scope],
    )
    headers = {"Authorization": f"Bearer {token}"}

    async with httpx.AsyncClient() as client:
        response = await client.post(url, json=body, headers=headers)

    logger.info(response.text)

    if response.is_error:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, detail=response.text
        )

    return response.json()
