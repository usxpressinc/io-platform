import logging

from fastapi import HTTPException, status

from src.helpers.geoservices import get_location_match
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


def lookup_jobs(item: models.JobLookupRequest) -> models.JobLookupResponse:
    try:
        linked_kml_href = settings.Nora_GoogleMapsKml
        linked_kml_bytes = helpers.download_linked_kml(linked_kml_href)
        polygons = helpers.extract_polygons_from_kml(linked_kml_bytes)
        data = get_location_match(item.city, item.state)
        jobs = helpers.get_jobs_point_against_polygons(
            polygons=polygons,
            latitude=data["Match"]["Y"],
            longitude=data["Match"]["X"],
            distanceFromJob=item.milesFromJob,
        )
        return models.JobLookupResponse(jobs=jobs)
    except Exception as e:
        logger.error("Error:", repr(e))
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Failed with error {repr(e)}",
        )
