from enum import Enum
from typing import Optional

from pydantic import BaseModel, ConfigDict

from src.models.common import BaseCleanModel


class LatLongRequest(BaseCleanModel):
    latitude: float | None = None
    longitude: float | None = None


class TypeEnum(str, Enum):
    truck = "truck"
    trailer = "trailer"


class QueryRequest(LatLongRequest):
    model_config = ConfigDict(extra="forbid")

    address: str | None = None
    city: str | None = None
    state: str | None = None
    zip: str | None = None


class VendorLookupRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    requiredService: str = ""
    radiusInMiles: float = 80467
    location: QueryRequest
    type: TypeEnum = TypeEnum.truck


class Vendor(BaseCleanModel):
    supplierCode: Optional[str] = None
    name: Optional[str] = None
    hoursOfOperation: Optional[int] = None
    hoursOfOperationString: Optional[str] = None
    rating: Optional[str] = None
    paymentMethod: Optional[str] = None
    paymentTerms: Optional[str] = None
    location: Optional[str] = None
    address1: Optional[str] = None
    address2: Optional[str] = None
    city: Optional[str] = None
    state: Optional[str] = None
    zip: Optional[str] = None
    country: Optional[str] = None
    phone: Optional[str] = None
    fax: Optional[str] = None
    email: Optional[str] = None
    contact: Optional[str] = None
    latitude: Optional[float] = None
    longitude: Optional[float] = None
    laborRate: Optional[float] = None
    stat: Optional[str] = None
    currencyType: Optional[str] = None
    supplierGroupParameter: int
    milesFromTruck: Optional[float] = None
    milesFromTruckMessage: Optional[str] = None
    milesFromTrailer: Optional[float] = None
    milesFromTrailerMessage: Optional[str] = None
    timeZone: Optional[str] = None


class VendorLookupResponse(BaseCleanModel):
    vendors: list[Vendor | str] = []
    error: str | None = None


class VendorServiceMap(BaseModel):
    code: int
    services: list[str]


class VendorService(BaseModel):
    label: str
    serviceKey: int


class VendorServiceCode(BaseModel):
    code: int
    services: list[int]


schema = VendorLookupResponse(
    error="If there is some error",
    vendors=[
        """Vendor Details
    """
    ],
)
