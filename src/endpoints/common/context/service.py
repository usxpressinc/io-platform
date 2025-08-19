import datetime
import json
import logging
import re

from fastapi import HTTPException
from mongoengine import connect

from src.endpoints.larry.vendor_lookup import vendor_service
from src.helpers import orders
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_context(id: str | None = None, number: str | None = None) -> dict:
    """
    Retrieve driver context information including driver details, truck/trailer data, and recent call history.

    Args:
        id: Optional driver ID for lookup
        number: Optional phone number for driver lookup (will be normalized)

    Returns:
        dict: Context response containing driver information, truck/trailer details, and call data

    Raises:
        HTTPException: If driver data is not found (404)
    """
    # Normalize phone number by removing non-digits and US country code if present
    if number is not None:
        number = clean_number(number)
    driver_response = await helpers.get_driver_context(id=id, number=number)
    d_data = driver_response.driverdata
    if d_data is None:
        raise HTTPException(status_code=404, detail="Driver data not found")
    driver = models.Driver(
        name=d_data.driverName or "",
        sbu=d_data.driverSBU or "",
        type=d_data.driverType or "",
        status=d_data.driverStatus or "",
        jobDesc=d_data.driverJobDesc or "",
    )
    # Add truck and trailer information if truck number exists
    if d_data.truckNumber != "":
        driver.truck = helpers.get_truck_location(
            company=d_data.truckCompany or "",
            number=d_data.truckNumber or "",
        )
        driver.trailer = helpers.get_trailer_location(
            truckCompany=d_data.truckCompany or "",
            truckNumber=d_data.truckNumber or "",
        )
        # Calculate total weight from order if order number exists
        if d_data.orderNumber is not None:
            order = await orders.search_order_by_number(
                number=d_data.orderNumber
            )
            logger.info(order)
            weightsArray: list[list[int]] = order["data"]["items"][0].get(
                "weights", []
            )
            weights = 0
            for w in weightsArray:
                weights += sum(w)
            driver.trailer.weight = weights
    driver.vendor_services = await vendor_service.get_services()
    res = models.ContextResponse(driver=driver)

    if number is not None:
        # Connect to MongoDB and retrieve recent call context (within last 5 minutes)
        connect(
            host=f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}&tls=true",
            db="hrob-poc",
        )
        current_time = datetime.datetime.now()
        five_minutes_ago = current_time - datetime.timedelta(minutes=5)
        context: models.ContextDb | None = models.ContextDb.objects(  # type: ignore
            number=number,
            date_modified__gte=five_minutes_ago,
        ).first()
        logger.info(context)
        if context is not None:
            res.call = context.data  # type: ignore

    return res.model_dump()


async def post_context(
    number: str, corelation_id: str, data: dict | None = None
) -> dict:
    """
    Store context data in MongoDB for a given phone number and correlation ID.

    Args:
        number: Phone number associated with the context
        corelation_id: Unique correlation identifier for the context entry
        data: Optional dictionary containing context data to store

    Returns:
        dict: JSON representation of the saved context document
    """
    if number is not None:
        number = clean_number(number)
    # Connect to MongoDB with TLS configuration
    logger.info(
        f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}"
    )
    connect(
        host=f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}&tls=true",
        db="hrob-poc",
    )
    logger.info("connected")
    # Create, save, and reload context document
    context = models.ContextDb(number=number, id=corelation_id)
    context.data = data
    context.save()
    context.reload()
    return json.loads(context.to_json())


def clean_number(number: str) -> str:
    """
    Normalize a phone number by removing non-digit characters and US country code if present.

    Args:
        number: Phone number string to normalize

    Returns:
        str: Normalized phone number containing only digits
    """
    digits = re.sub(r"[^0-9]", "", number)
    return (
        digits[1:] if len(digits) == 11 and digits.startswith("1") else digits
    )
