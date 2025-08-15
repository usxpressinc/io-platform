import logging
import typing

from src.settings import Settings

from . import helpers
from .models import response as response_model

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def lookup_price(body: typing.Any) -> response_model.LookupPriceResponse:
    stops = []
    for key, value in body.items():
        if key.startswith("stops."):
            seq = int(key.split(".", 1)[1])
            stops.append({"seq": seq, **value})
        else:
            value = helpers.parse_object(value=value)
    body = {k: v for k, v in body.items() if not k.startswith("stops.")}

    # Sort the list by sequence number
    stops.sort(key=lambda x: x["seq"])
    body["stops"] = stops
    logger.debug("SPAPI Request Body : %s", body)

    response = await helpers.get_price(body=body)
    pricing_data = response.get("data", dict()).get("pricing", [])
    logger.debug("SPAPI Response Body : %s", pricing_data)

    price_item = next(
        (x for x in pricing_data if x["name"] == "01-BROKERAGE"),
        dict(),
    )
    logger.debug("SPAPI PriceItem : %s", price_item)
    result = response_model.LookupPriceResponse(
        allInPrice=typing.cast(float, price_item["price"]["allInPrice"]),
        distance=typing.cast(float, price_item["price"]["distance"]),
        basePrice=typing.cast(float, price_item["price"]["cost"]),
    )

    return result
