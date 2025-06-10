from fastapi import APIRouter, Security, status

from src.helpers.auth import authenticate_token

from .context import context_models, context_service
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


@router.post(
    "/context",
    summary="Get User Context",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=context_models.ContextResponse,
)
async def get_user_context(
    item: context_models.ContextRequest,
    authenticated: bool = Security(authenticate_token, scopes=["common"]),
) -> context_models.ContextResponse:
    return await context_service.get_context(item=item)
