import logging

import requests
from fastapi import Security, status
from fastkml import Placemark, kml
from fastkml.utils import find_all
from pydantic import BaseModel
from pygeoif import geometry
from pyproj import Transformer
from shapely.geometry import Point, Polygon

from src.endpoints.router import router
from src.helpers import geoservices
from src.helpers.auth import authenticate_token
from src.settings import Settings

settings = Settings()
logger = logging.getLogger(__name__)


class Job(BaseModel):
    name: str
    description: str


class JobLookupResponse(BaseModel):
    jobs: list[Job] = []


class JobLookupRequest(BaseModel):
    city: str
    state: str


class JobCoords(BaseModel):
    lat: float
    lon: float


class JobLocation(BaseModel):
    name: str
    description: str
    coords: list[JobCoords]


def download_linked_kml(href):
    if href.startswith("http"):
        response = requests.get(href)
        response.raise_for_status()
        return response.text
    else:
        with open(href, "r", encoding="utf-8") as f:
            return f.read()


# === Step 3: Parse KML and extract polygons ===


def parse_geometries(placemark: Placemark):
    coords: list[JobCoords] = []
    geom = placemark.geometry
    if isinstance(geom, geometry.Point):
        for _ in range(5):
            coords.append(JobCoords(lat=geom.x, lon=geom.y))
    elif isinstance(geom, geometry.LineString) or isinstance(
        geom, geometry.LinearRing
    ):
        for coordinates in geom.coords:
            coords.append(JobCoords(lat=coordinates[0], lon=coordinates[1]))
    elif isinstance(geom, geometry.Polygon):
        for coordinates in geom.exterior.coords:
            coords.append(JobCoords(lat=coordinates[0], lon=coordinates[1]))
        for interior in geom.interiors:
            for coordinates in interior.coords:
                coords.append(JobCoords(lat=coordinates[0], lon=coordinates[1]))
    elif isinstance(geom, geometry.MultiGeometry):
        for g in geom.geoms:
            coords.append(JobCoords(lat=g.x, lon=g.y))
    return coords


def extract_polygons_from_kml(kml_str):
    k = kml.KML.from_string(kml_str)
    # logger.info(kml_str)
    polygons: list[JobLocation] = []

    try:
        placemarks: list[Placemark] = list(find_all(k, of_type=Placemark))
        for p in placemarks:
            coords = parse_geometries(p)

            polygons.append(
                JobLocation(
                    name=p.name, description=p.description, coords=coords
                )
            )
    except Exception as e:
        logger.error(repr(e))
    return polygons


# === Step 4: Project & check point ===


def get_jobs_point_against_polygons(
    polygons: list[JobLocation], latitude: float, longitude: float
) -> list[Job]:
    transformer = Transformer.from_crs("EPSG:4326", "EPSG:3857", always_xy=True)
    x, y = transformer.transform(longitude, latitude)
    point = Point(x, y)

    jobs: list[Job] = []

    for i, poly in enumerate(polygons):
        print(poly.name)
        coords = [transformer.transform(p.lon, p.lat) for p in poly.coords]
        metric_polygon = Polygon(coords)

        good_job = False
        if metric_polygon.contains(point):
            logger.info("Point is inside polygon %s", i)
            good_job = True
        else:
            distance = point.distance(metric_polygon)
            if distance * 0.000621371 <= settings.Nora_MilesFromJob:
                logger.info(
                    f"Point is within {distance:.2f} meters of polygon {i}"
                )
                good_job = True
            else:
                logger.warning(
                    f"Point is {distance:.2f} meters away from polygon {i}"
                )
        if good_job:
            jobs.append(Job(name=poly.name, description=poly.description))
    return jobs


@router.post(
    "/v1/nora/jobs/lookup",
    tags=["nora"],
    summary="Get list of jobs for an endpoint",
    response_description="Return HTTP Status Code 200 (OK)",
    status_code=status.HTTP_200_OK,
    response_model=JobLookupResponse,
)
def lookup_jobs(
    item: JobLookupRequest,
    authenticated: bool = Security(authenticate_token, scopes=["nora"]),
) -> JobLookupResponse:
    try:
        linked_kml_href = settings.Nora_GoogleMapsKml
        linked_kml_str = download_linked_kml(linked_kml_href)
        polygons = extract_polygons_from_kml(linked_kml_str)
        data = geoservices.get_location_match(item.city, item.state)
        jobs = get_jobs_point_against_polygons(
            polygons, data["Match"]["X"], data["Match"]["Y"]
        )
        return JobLookupResponse(jobs=jobs)
    except Exception as e:
        logger.error("Error:", repr(e))
