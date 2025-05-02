import logging

import requests

from .models import pricing

logger = logging.getLogger(__name__)


def get_price(item: pricing.PriceRequest):
    url = "https://api.pricing.spot.internal.usxpress.io/v3/pricing/price"

    # TODO: Do not set verify = False in production
    response = requests.post(url, json=item.model_dump_json())

    response.raise_for_status()

    return response.json()
