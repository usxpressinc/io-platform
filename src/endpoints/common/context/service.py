import json
import logging

from mongoengine import connect

from src.endpoints.larry.vendor_lookup import vendor_service
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_context(id: str | None = None, number: str | None = None) -> dict:
    connect(host=settings.MongoDbConnectionString, db="hrob-poc")
    contexts: list[models.ContextDb] = models.ContextDb.objects(id=id, number=number)  # type: ignore
    if contexts.count == 1:
        return json.loads(contexts[0].to_json())

    driver_response = await helpers.get_driver_context(id=id, number=number)
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
    return models.ContextResponse(context=driver).model_dump()


async def post_context(
    number: str, corelation_id: str, data: dict | None = None
) -> dict:
    connect(host=settings.MongoDbConnectionString, db="hrob-poc")
    context = models.ContextDb(number=number, id=corelation_id)
    context.data = data
    context.save()
    context.reload()
    return json.loads(context.to_json())
