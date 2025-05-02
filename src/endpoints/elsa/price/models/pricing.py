from typing import List

from pydantic import BaseModel


class PriceLoadDetails(BaseModel):
    source: str | None
    customerCode: str | None
    contractedCompanyCode: str | None
    loadId: str | None
    distance: str | None
    equipmentType: str | None
    hazmat: bool | None
    tempProtect: bool | None
    tankerEndorsement: bool | None
    hvp: bool | None
    scac: str | None
    shipmentID: str | None
    requestedCostType: str | None
    truckCount: int | None
    teamRequired: bool | None
    buyItNow: float | None
    company: str | None
    customerNumber: int | None
    userId: str | None


class PriceStop(BaseModel):
    sequenceNumber: int | None
    stopType: str | None
    name: str | None
    loadType: str | None
    apptEarlyDateTime: str | None
    apptLateDateTime: str | None
    streetAddress1: str | None
    streetAddress2: str | None
    city: str | None
    state: str | None
    postalCode: str | None
    country: str | None
    lat: float | None
    long: float | None
    distance: str | None
    pickUpEarlyDateTime: str | None
    pickUpLateDateTime: str | None


class PriceRequest(BaseModel):
    loadDetails: PriceLoadDetails
    stops: List[PriceStop]
    requestSource: str | None
