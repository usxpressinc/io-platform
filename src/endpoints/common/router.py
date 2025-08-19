from fastapi import APIRouter, Response, Security, status
from fastapi.responses import JSONResponse

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


@router.get(
    "/context",
    summary="Get User Context",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=context_models.ContextResponse,
)
async def get_user_context(
    authenticated: bool = Security(authenticate_token, scopes=["common"]),
    id: str | None = None,
    number: str | None = None,
    callContext: bool = False,
) -> Response:
    content = await context_service.get_context(
        id=id, number=number, callContext=callContext
    )
    return JSONResponse(content=content, status_code=200)


@router.post(
    "/context",
    summary="Add User Context",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=dict(),
)
async def post_user_context(
    item: context_models.ContextRequest,
    authenticated: bool = Security(authenticate_token, scopes=["common"]),
) -> Response:
    content = await context_service.post_context(
        id=item.id, number=item.number, data=item.data
    )
    return JSONResponse(content=content, status_code=200)
