import logging

from fastapi import HTTPException, status

from src.helpers.geoservices import get_location_match
from src.settings import Settings

from . import helpers, models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def lookup_jobs(
    item: models.JobLookupRequest,
) -> models.JobLookupResponse:
    try:
        data = await get_location_match(item.city, item.state)
        jobs = helpers.get_jobs_point_against_polygons(
            polygons=helpers.Lea_Polygons,
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
