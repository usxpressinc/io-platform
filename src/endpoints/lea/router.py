from fastapi import APIRouter, Security, status

from src.helpers.auth import authenticate_token

from .jobs_lookup import jobs_models, jobs_service

router = APIRouter(prefix="/api/lea", include_in_schema=True, tags=["lea"])


@router.post(
    "/jobs/lookup",
    summary="Get list of jobs for an endpoint",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=jobs_models.JobLookupResponse,
)
def lookup_jobs(
    item: jobs_models.JobLookupRequest,
    authenticated: bool = Security(authenticate_token, scopes=["lea"]),
) -> jobs_models.JobLookupResponse:
    return jobs_service.lookup_jobs(item=item)
