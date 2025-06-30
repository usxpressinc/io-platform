import logging

from src.endpoints.larry.vendor_lookup import vendor_service
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_context(item: models.ContextRequest) -> models.ContextResponse:
    driver_response = await helpers.get_driver_context(item.id, item.phone)
    d_data = driver_response.driverdata
    driver = models.Driver(
        name=d_data.driverName,
        sbu=d_data.driverSBU,
        type=d_data.driverType,
        status=d_data.driverStatus,
        jobDesc=d_data.driverJobDesc,
    )
    if d_data.truckNumber != "":
        driver.truck = helpers.get_truck_location(
            company=d_data.truckCompany,
            number=d_data.truckNumber,
        )
        driver.trailer = helpers.get_trailer_location(
            truckCompany=d_data.truckCompany,
            truckNumber=d_data.truckNumber,
        )
    driver.vendor_services = await vendor_service.get_services()
    return models.ContextResponse(context=driver)
