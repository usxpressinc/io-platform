import datetime
import json
import logging
import re

from fastapi import HTTPException
from mongoengine import Q, connect

from src.endpoints.larry.vendor_lookup import vendor_service
from src.helpers import orders
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def _calculate_trailer_weight(order_number: int) -> int:
    """Calculate total weight from order items."""
    order = await orders.search_order_by_number(number=order_number)
    logger.info(order)
    weights_array: list[list[int]] = order["data"]["items"][0].get(
        "weights", []
    )
    return sum(sum(weight_row) for weight_row in weights_array)


async def _create_driver_with_vehicle_info(d_data) -> models.Driver:
    """Create driver object with truck and trailer information if available."""
    driver = models.Driver(
        name=d_data.driverName or "",
        sbu=d_data.driverSBU or "",
        type=d_data.driverType or "",
        status=d_data.driverStatus or "",
        jobDesc=d_data.driverJobDesc or "",
        id=d_data.driverID or "",
        company=d_data.driverCompany or "",
        fleet=models.Fleet(
            manager=d_data.fleetManager or "",
            serviceCenter=d_data.fleetServiceCenter or "",
            owner=d_data.fleetOwner or "",
        ),
        training=models.Training(
            coordinator=d_data.trainingCoordinator or "",
            coordinatorSupervisor=d_data.trainingCoordinatorSupervisor or "",
        ),
        stateZone=d_data.stateZone or None,
        order=models.Order(
            number=d_data.orderNumber or None,
            sbu=d_data.orderSBU or None,
            terminal=d_data.orderTerminal or None,
        ),
        primaryCoverage=d_data.primaryCoverage or None,
        domicileTerminal=d_data.domicileTerminal or None,
        currentPTA=d_data.currentPTA or None,
        preferredLanguage=d_data.preferredLanguage or None,
    )

    if d_data.truckNumber:
        driver.truck = helpers.get_truck_location(
            company=d_data.truckCompany or "",
            number=d_data.truckNumber or "",
        )
        driver.trailer = helpers.get_trailer_location(
            truckCompany=d_data.truckCompany or "",
            truckNumber=d_data.truckNumber or "",
        )

        if d_data.orderNumber:
            driver.trailer.weight = await _calculate_trailer_weight(
                d_data.orderNumber
            )

    driver.vendor_services = await vendor_service.get_services()
    return driver


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
    response = models.ContextResponse()

    if number:
        number = clean_number(number)

    driver_response = await helpers.get_driver_context(id=id, number=number)
    d_data = driver_response.driverdata

    if not d_data:
        raise HTTPException(status_code=404, detail="Driver data not found")

    if d_data:
        response.driver = await _create_driver_with_vehicle_info(d_data)

    return response.model_dump()


async def get_genesys_driver_context(
    id: str | None = None, number: str | None = None
) -> dict:
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
    response = models.GenesysDriverResponse()

    if number:
        number = clean_number(number)
    connect(
        host=f"{settings.MongoDbConnectionString}&tlsCertificateKeyFile={settings.MongoDbTlsFile}&tls=true",
        db="hrob-poc",
    )
    current_time = datetime.datetime.now()
    five_minutes_ago = current_time - datetime.timedelta(minutes=5)
    context: models.ContextDb | None = models.ContextDb.objects(  # type: ignore
        (Q(id=id) | Q(number=number))
        & Q(date_modified__gte=five_minutes_ago.isoformat() + "Z")
    ).first()
    logger.info(context)
    if context:
        response.call = context.data  # type: ignore
        id = context.id  # type: ignore
        number = ""
        logger.info(f"Found context for {id} or {number}")

    driver_response = await helpers.get_driver_context(id=id, number=number)
    d_data = driver_response.driverdata

    if not d_data:
        raise HTTPException(status_code=404, detail="Driver data not found")

    response.driver = d_data.model_dump()

    return response.model_dump()


async def post_context(number: str, id: str, data: dict | None = None) -> dict:
    """
    Store context data in MongoDB for a given phone number and correlation ID.

    Args:
        number: Phone number associated with the context
        id: ID for the context entry
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
    context = models.ContextDb(
        number=number,
        id=id,
        date_modified=datetime.datetime.now(datetime.UTC).isoformat() + "Z",
    )
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
