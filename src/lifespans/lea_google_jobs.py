import logging

from src.endpoints.lea.jobs_lookup import helpers
from src.settings import Settings

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


def get_jobs():
    try:
        logger.info("Getting Lea Jobs from KML")
        linked_kml_href = settings.Nora_GoogleMapsKml
        linked_kml_bytes = helpers.download_linked_kml(linked_kml_href)
        helpers.Lea_Polygons = helpers.extract_polygons_from_kml(
            linked_kml_bytes
        )
        logger.info("Completed getting Lea Jobs from KML")
    except Exception as e:
        logger.error("Error getting Lea Jobs from KML:", repr(e))
