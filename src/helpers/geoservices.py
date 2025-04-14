import requests
from fastapi import HTTPException, status


def get_location_match(city: str | None = None, state: str | None = None):
    url = "https://geoservices.usxpress.com/api/locations/match"

    params = {"city": city, "state": state}

    # TODO: Do not set verify = False in production
    response = requests.get(url, params=params, verify=False)

    if not response.ok:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST)

    return response.json()
