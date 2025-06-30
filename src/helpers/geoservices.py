import httpx
from fastapi import HTTPException, status


async def get_location_match(
    city: str | None = None, state: str | None = None, postal: str | None = None
):
    url = "https://geoservices.usxpress.com/api/locations/match"

    params = {"city": city, "state": state, "postal": postal}

    # TODO: Do not set verify = False in production
    async with httpx.AsyncClient(verify=False) as client:
        response = await client.get(url, params=params)

    if response.is_error:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, detail=response.text
        )

    return response.json()
