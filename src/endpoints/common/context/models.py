import datetime
import uuid

from mongoengine import DictField, Document, StringField
from pydantic import BaseModel, ConfigDict


class ContextRequest(BaseModel):
    id: str
    number: str
    data: dict


class Location(BaseModel):
    company: str | None = None
    number: str | None = None
    latitude: float | None = None
    longitude: float | None = None
    weight: float | None = None


class Fleet(BaseModel):
    manager: str
    owner: str
    serviceCenter: str


class Training(BaseModel):
    coordinator: str
    coordinatorSupervisor: str


class Order(BaseModel):
    number: int | None = None
    sbu: str | None = None
    terminal: str | None = None


class Driver(BaseModel):
    model_config = ConfigDict(str_strip_whitespace=True)

    id: str | None = None
    company: str | None = None
    name: str
    sbu: str
    type: str
    status: str
    jobDesc: str
    persona: str = "driver"
    truck: Location | None = None
    trailer: Location | None = None
    vendor_services: list[str] = []
    fleet: Fleet | None = None
    training: Training | None = None
    stateZone: str | None = None
    order: Order | None = None
    primaryCoverage: str | None = None
    domicileTerminal: str | None = None
    currentPTA: str | None = None
    preferredLanguage: str | None = None


class ContextResponse(BaseModel):
    driver: Driver | None = None


class GenesysDriverResponse(BaseModel):
    driver: dict | None = None
    call: dict | None = None


class DriverContext(BaseModel):
    driverID: str | None = None
    driverCompany: str | None = None
    driverName: str | None = None
    truckCompany: str | None = None
    truckNumber: str | None = None
    employerCode: str | None = None
    fleetManager: str | None = None
    fleetServiceCenter: str | None = None
    fleetOwner: str | None = None
    trainingCoordinator: str | None = None
    trainingCoordinatorSupervisor: str | None = None
    driverJobDesc: str | None = None
    stateZone: str | None = None
    orderNumber: int | None = None
    primaryCoverage: str | None = None
    driverSBU: str | None = None
    driverType: str | None = None
    orderSBU: str | None = None
    orderTerminal: str | None = None
    domicileTerminal: str | None = None
    driverStatus: str | None = None
    currentPTA: str | None = None
    truckLocation: str | None = None
    preferredLanguage: str | None = None


class DriverData(BaseModel):
    driverdata: DriverContext | None = None


class ContextDb(Document):
    id = StringField(required=True, default=uuid.uuid4(), primary_key=True)
    number = StringField(required=True)
    data = DictField()
    date_modified = StringField(
        default=datetime.datetime.now(datetime.UTC).isoformat() + "Z"
    )
    meta = {"collection": "hrob-context"}
