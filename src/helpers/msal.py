import logging
import typing

from msal import ConfidentialClientApplication

logger = logging.getLogger(__name__)


async def get_access_token(
    client_id: str, client_secret: str, tenant: str, scopes: list[str]
) -> str:
    app = ConfidentialClientApplication(
        client_id=client_id,
        client_credential=client_secret,
        authority=f"https://login.microsoftonline.com/{tenant}",
    )

    # Acquire token
    result = app.acquire_token_for_client(scopes=scopes)
    r = typing.cast(dict[str, str], result)

    if "access_token" in r:
        return r["access_token"]
    logger.error(r)
    return ""
