import json
import logging

from mongoengine import connect

from src.endpoints.larry.vendor_lookup import vendor_service
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_context(id: str | None = None, number: str | None = None) -> dict:
    connect(
        host=f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}&tls=true",
        db="hrob-poc",
    )
    context: list[models.ContextDb] = models.ContextDb.objects(number=number).first()  # type: ignore
    logger.info(context)
    if context is not None:
        return context.data  # type: ignore

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
    logger.info(
        f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}"
    )
    connect(
        host=f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}&tls=true",
        db="hrob-poc",
    )
    logger.info("connected")
    context = models.ContextDb(number=number, id=corelation_id)
    context.data = data
    context.save()
    context.reload()
    return json.loads(context.to_json())
