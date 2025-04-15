from fastapi import APIRouter, Body, Security, status
from pydantic import EmailStr

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
    from_email: EmailStr,
    to_email: EmailStr,
    subject: str,
    body: str = Body(..., media_type="text/plain"),
    authenticated: bool = Security(authenticate_token, scopes=["common"]),
) -> email_models.SendEmailResponse:
    return email_service.send_email(
        item=email_models.SendEmailRequest(
            from_email=from_email, to_email=to_email, subject=subject, body=body
        )
    )
