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

def parse_object(value: typing.Any) -> typing.Any:
    if isinstance(value, str):
        if value.lower() == "false":
            return False
        elif value.lower() == "true":
            return True
        try:
            return int(value)
        except ValueError:
            pass
        try:
            return float(value)
        except ValueError:
            pass
    if isinstance(value, dict):
        for k, v in value.items():
            value[k] = parse_object(v)
        return value
    if isinstance(value, list):
        return [parse_object(item) for item in value]
    return value
