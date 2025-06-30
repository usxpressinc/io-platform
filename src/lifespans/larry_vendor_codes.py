import logging

from src.endpoints.larry.vendor_lookup import helpers
from src.settings import Settings

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def vendor_codes():
    try:
        logger.info("Getting Vendor Codes Map")
        codes = await helpers.vendors_service_codes()
        services = await helpers.vendors_services()
        services_map = dict()
        for service in services:
            services_map[service.serviceKey] = service.label.lower()
        service_code_mapper: dict[int, list[str]] = dict()
        for code in codes:
            service_code_mapper[code.code] = [
                services_map[x] for x in code.services
            ]
        helpers.VendorServiceMap = service_code_mapper
        logger.info("Completed mapping Vendor Codes %s", service_code_mapper)
    except Exception as e:
        logger.error("Error mapping Vendor Codes:", repr(e))
