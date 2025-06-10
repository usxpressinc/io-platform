import logging

from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_context(item: models.ContextRequest) -> models.ContextResponse:
    # print(helpers.get_truck_location())
    driver_response = await helpers.get_driver_context(item.id, item.phone)
    driver = models.Driver(
        name=driver_response.driverdata.driverName,
        sbu=driver_response.driverdata.driverSBU,
        type=driver_response.driverdata.driverType,
        status=driver_response.driverdata.driverStatus,
        jobDesc=driver_response.driverdata.driverJobDesc,
    )
    truck = None
    trailer = None
    if driver_response.driverdata.truckNumber != "":
        truck = helpers.get_truck_location(
            company=driver_response.driverdata.truckCompany,
            number=driver_response.driverdata.truckNumber,
        )
        print(truck)
        trailer = helpers.get_trailer_location(
            truckCompany=driver_response.driverdata.truckCompany,
            truckNumber=driver_response.driverdata.truckNumber,
        )
        print(trailer)
    return models.ContextResponse(caller=driver, truck=truck, trailer=trailer)
