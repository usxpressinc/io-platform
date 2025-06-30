import httpx
from fastapi import HTTPException, status
from src.settings import Settings

settings = Settings.model_validate({})


async def get_order(
    id: str
):
    url = f"{settings.Orders_Url}/v1/orders/{id}"

    async with httpx.AsyncClient() as client:
        response = await client.get(url)

    if response.is_error:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST, detail=response.text
        )

    return response.json()
