import logging
from typing import cast

import requests
from fastkml import Placemark, kml
from fastkml.utils import find_all
from pygeoif import geometry
from pyproj import Transformer
from shapely.geometry import Point, Polygon

from . import models

logger = logging.getLogger(__name__)


def download_linked_kml(href):
    if href.startswith("http"):
        response = requests.get(href)
        response.raise_for_status()
        return response.content
    else:
        with open(href, "r", encoding="utf-8") as f:
            return f.read()


def parse_geometries(placemark: Placemark):
    coords: list[models.JobCoords] = []
    geom = placemark.geometry
    if isinstance(geom, geometry.Point):
        for _ in range(5):
            coords.append(models.JobCoords(lat=geom.y, lon=geom.x))
    elif isinstance(geom, geometry.LineString) or isinstance(
        geom, geometry.LinearRing
    ):
        for coordinates in geom.coords:
            coords.append(
                models.JobCoords(lat=coordinates[1], lon=coordinates[0])
            )
    elif isinstance(geom, geometry.Polygon):
        for coordinates in geom.exterior.coords:
            coords.append(
                models.JobCoords(lat=coordinates[1], lon=coordinates[0])
            )
        for interior in geom.interiors:
            for coordinates in interior.coords:
                coords.append(
                    models.JobCoords(lat=coordinates[1], lon=coordinates[0])
                )
    elif isinstance(geom, geometry.MultiPolygon):
        for g in geom.geoms:
            for coordinates in g.exterior.coords:
                coords.append(
                    models.JobCoords(lat=coordinates[1], lon=coordinates[0])
                )
            for interior in g.interiors:
                for coordinates in interior.coords:
                    coords.append(
                        models.JobCoords(lat=coordinates[1], lon=coordinates[0])
                    )
    return coords


def extract_polygons_from_kml(kml_bytes):
    k = kml.KML.from_string(kml_bytes)
    polygons: list[models.JobLocation] = []

    raw: list[object] = list(find_all(k, of_type=Placemark))
    placemarks: list[Placemark] = cast(list[Placemark], raw)
    for p in placemarks:
        if p.name is not None:
            if "closed" in p.name.lower():
                continue
            coords = parse_geometries(p)

            polygons.append(
                models.JobLocation(
                    name=p.name, description=p.description, coords=coords
                )
            )
    return polygons


def get_jobs_point_against_polygons(
    polygons: list[models.JobLocation],
    latitude: float,
    longitude: float,
    distanceFromJob: float,
) -> list[models.Job]:
    transformer = Transformer.from_crs("EPSG:4326", "EPSG:3857", always_xy=True)
    x, y = transformer.transform(longitude, latitude)
    point = Point(x, y)

    logger.info("Lat, Long is %s %s", latitude, longitude)

    jobs: list[models.Job] = []

    for i, poly in enumerate(polygons):
        coords = [transformer.transform(p.lon, p.lat) for p in poly.coords]
        metric_polygon = Polygon(coords)

        good_job = False
        distance = 0
        if metric_polygon.contains(point):
            logger.info("Point %s is inside polygon %s", poly.name, i)
            good_job = True
        else:
            distance = point.distance(metric_polygon) * 0.000621371
            if distance <= distanceFromJob:
                logger.info(
                    f"models.Job {poly.name} is within {distance:.2f} miles of polygon {i}"
                )
                good_job = True
            else:
                logger.warning(
                    f"models.Job {poly.name} is {distance:.2f} miles away from polygon {i}"
                )
        if good_job:
            jobs.append(
                models.Job(
                    name=poly.name,
                    description=poly.description,
                    milesFromLocation=distance,
                )
            )
    return jobs
