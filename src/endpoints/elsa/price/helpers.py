import logging
import typing

import requests

from src.settings import Settings

logger = logging.getLogger(__name__)

settings = Settings.model_validate({})


def get_price(body: typing.Any) -> dict:
    url = settings.Elsa_pricing_api

    response = requests.post(url, json=body)

    response.raise_for_status()

    return response.json()
