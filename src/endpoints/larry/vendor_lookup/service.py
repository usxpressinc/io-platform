import logging
import math

from src.helpers.geoservices import get_location_match
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_services() -> list[str]:
    result: list[str] = []
    services = await helpers.vendors_services()
    for s in services:
        result.append(s.label.lower())
    return result


async def lookup_vendors(
    item: models.VendorLookupRequest,
) -> models.VendorLookupResponse:
    if (item.location.latitude is None or item.location.longitude is None) and (
        (item.location.city is not None and item.location.state is not None)
        or item.location.zip is not None
    ):
        loc = await get_location_match(
            city=item.location.city,
            state=item.location.state,
            postal=item.location.zip,
        )
        item.location.latitude = loc["Match"]["Y"]
        item.location.longitude = loc["Match"]["X"]
    data = {
        "queryLocation": item.location.model_dump(exclude_none=True),
        "bufferDistance": math.ceil(item.radiusInMiles * 1609.344),
    }

    data[f"{item.type.value}Location"] = {
        "latitude": item.location.latitude,
        "longitude": item.location.longitude,
    }
    vendors = await helpers.vendors_by_location(data)
    result: list[models.Vendor | str] = []
    for vendor in vendors:
        print(
            vendor.supplierGroupParameter,
            item.requiredService,
            item.requiredService.lower()
            in helpers.VendorServiceMap[int(vendor.supplierGroupParameter)],
        )
        if (
            item.requiredService.lower()
            in helpers.VendorServiceMap[int(vendor.supplierGroupParameter)]
        ):
            result.append(vendor)
    return models.VendorLookupResponse(vendors=result)
