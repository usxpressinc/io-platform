from fastapi import Depends, HTTPException, status
from fastapi.security import APIKeyHeader, SecurityScopes

api_header = APIKeyHeader(name="authorization")


def authenticate_token(
    scopes: SecurityScopes, token: str = Depends(api_header)
) -> bool:
    if token not in scopes.scopes:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail={
                "code": "not_authorized",
                "description": "User is not allowed to access this endpoint.",
            },
        )
    return True
