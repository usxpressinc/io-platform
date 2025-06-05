import asyncio
import logging
from datetime import timedelta

from src.settings import Settings

LIFETIME = timedelta(hours=10)
REFRESH = timedelta(hours=9)
TARGET_URL = "http://example.org"

logger = logging.getLogger(__name__)
settings = Settings.model_validate({})


async def run_kinit(renew: bool = False) -> None:
    """
    renew=True  → `kinit -R`
    renew=False → `kinit user@REALM` (password from constant)
    Raises RuntimeError if kinit exits non-zero.
    """
    cmd = ["kinit", "-V"]  # -F = forwardable (best practice)
    if renew:
        cmd.append("-R")
    else:
        cmd += ["-l", str(int(LIFETIME.total_seconds()))]  # guarantee lifetime
        cmd.append(settings.Larry_xpm_api_principal)

    proc = await asyncio.create_subprocess_exec(
        *cmd,
        stdin=asyncio.subprocess.PIPE if not renew else None,
        stdout=asyncio.subprocess.PIPE,
        stderr=asyncio.subprocess.PIPE,
    )
    stdin_data = (
        (settings.Larry_xpm_api_password + "\n").encode() if not renew else None
    )
    stdout, stderr = await proc.communicate(stdin_data)

    logger.info("kinit %s %s", stderr.decode().strip(), stdout.decode().strip())

    if proc.returncode != 0:
        raise RuntimeError(
            f"kinit {'-R' if renew else ''} failed: "
            f"{stderr.decode().strip() or stdout.decode().strip()}"
        )


async def initial_kinit():
    logger.info("Getting initial TGT for %s", settings.Larry_xpm_api_principal)
    await run_kinit(renew=False)
    logger.info("Initial TGT acquired")


async def renew_ticket():
    try:
        logger.info("Attempting TGT renewal (-R)")
        await run_kinit(renew=True)
        logger.info("Ticket successfully renewed")
    except RuntimeError as e:
        logger.warning("%s — trying full kinit", e)
        await run_kinit(renew=False)
        logger.info("New TGT obtained after renewal failure")
