import logging
import uuid
from urllib.parse import urlencode, urljoin

import httpx
import pymssql
from fastapi import HTTPException
from httpx import HTTPStatusError
from pydantic import TypeAdapter

from src.settings import Settings

from . import models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


async def get_driver_context(
    id: str | None, number: str | None
) -> models.DriverData:
    url = urljoin(
        settings.Get_Driver_Host,
        "/sqllookup-exp-api-prod/api/v1/getbydriverinfo",
    )
    params = {
        "DriverNumber": id,
        "DriverPhoneNumber": number,
    }
    query_string = urlencode(params, doseq=True)
    url = f"{url}?{query_string}"

    try:
        async with httpx.AsyncClient() as client:
            response = await client.get(
                url=url,
                headers={
                    "authorization": f"Basic {settings.Get_Driver_Auth}",
                    "x-correlation-id": str(uuid.uuid4()),
                },
            )
        response.raise_for_status()
    except HTTPStatusError as e:
        logger.error(e.response.text)
        raise HTTPException(
            status_code=e.response.status_code, detail={"error": str(e)}
        )
    adapter = TypeAdapter(models.DriverData)
    return adapter.validate_python(response.json())


def get_truck_location(company: str, number: str) -> models.Location:
    user = settings.Kerberos_Principal.split("@")
    with pymssql.connect(
        host=settings.OpsServer,
        user=f"{user[1]}\\{user[0]}",
        password=settings.Kerberos_Password,
        database=settings.OpsDatabase,
    ) as conn:
        with conn.cursor() as cursor:
            cursor.execute(
                f"""
        SELECT TOP (1)
            PHVCO,
            PHVID,
            PHCLAT,
            PHCLON
        FROM [MobileComm].[dbo].[SMFD35_XPSFILE_Gen_Msg_Sys_Vehicle_Pos_Hist]
        WHERE PHVID = '{number}'
        AND PHVCO = '{company}'
        ORDER BY PHPOSD DESC, PHPOST DESC;
        """
            )
            row = cursor.fetchone()
            if row is not None:
                print(row)
                truck = models.Location(
                    company=str(row[0]),
                    number=row[1],
                    latitude=row[2],
                    longitude=row[3],
                )
                return truck
    raise HTTPException(
        status_code=404, detail=f"Truck not found for {company}:{number}"
    )


def get_trailer_location(
    truckCompany: str, truckNumber: str
) -> models.Location | None:
    user = settings.Kerberos_Principal.split("@")
    with pymssql.connect(
        host=settings.OpsServer,
        user=f"{user[1]}\\{user[0]}",
        password=settings.Kerberos_Password,
        database=settings.OpsDatabase,
    ) as conn:
        with conn.cursor() as cursor:
            cursor.execute(
                f"""
        SELECT
            tti.Company_Cd,
            tti.Trailer_Nbr,
            tti.Latitude_Nbr,
            tti.Longitude_Nbr
        FROM MobileComm.Trailer.TrailerTrackingInfo tti
        JOIN EnterpriseData.dbo.SMFD35_XPSFILE_Trailer_Assignment sxta
        ON tti.Company_Cd = sxta.TATRLCM
        AND tti.Trailer_Nbr = sxta.TATRL
        WHERE sxta.TASTATUS = 'ACTIVE'
        AND sxta.TATRCCM  = '{truckCompany}'
        AND sxta.TATRC = '{truckNumber}'
        """
            )
            row = cursor.fetchone()
            if row is not None:
                print(row)
                trailer = models.Location(
                    company=str(row[0]),
                    number=row[1],
                    latitude=row[2],
                    longitude=row[3],
                )
                return trailer
    return None
