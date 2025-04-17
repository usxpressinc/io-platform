from fastapi import APIRouter, HTTPException, Response, Security, status

from src.helpers.auth import authenticate_token
from src.models.response import LLMResponse

from .jobs_lookup import jobs_models, jobs_service

router = APIRouter(prefix="/api/lea", include_in_schema=True, tags=["lea"])


@router.post(
    "/jobs/lookup",
    summary="Get list of jobs for an endpoint",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=LLMResponse[jobs_models.JobLookupResponse],
)
def lookup_jobs(
    item: jobs_models.JobLookupRequest,
    response: Response,
    authenticated: bool = Security(authenticate_token, scopes=["lea"]),
) -> LLMResponse[jobs_models.JobLookupResponse]:
    try:
        result = jobs_service.lookup_jobs(item=item)
    except HTTPException as e:
        response.status_code = e.status_code
        result = e.detail
    return LLMResponse[jobs_models.JobLookupResponse](
        data=result, schema=jobs_models.schema
    )
