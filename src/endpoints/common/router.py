from fastapi import APIRouter, Security, status

from src.helpers.auth import authenticate_token

from .send_email import email_models, email_service

router = APIRouter(
    prefix="/api/common", include_in_schema=True, tags=["common"]
)


@router.post(
    "/email",
    summary="Send email",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=email_models.SendEmailResponse,
)
async def send_email(
    item: email_models.SendEmailRequest,
    authenticated: bool = Security(authenticate_token, scopes=["common"]),
) -> email_models.SendEmailResponse:
    return email_service.send_email(item=item)
