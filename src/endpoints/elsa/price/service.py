import logging
import typing

from src.settings import Settings

from . import helpers

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


def lookup_price(body: typing.Any) -> dict:
    return helpers.get_price(body=body)
