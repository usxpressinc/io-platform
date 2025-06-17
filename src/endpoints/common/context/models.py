from pydantic import BaseModel


class ContextRequest(BaseModel):
    id: str | None = None
    phone: str | None = None


class Location(BaseModel):
    company: str
    number: str
    latitude: float
    longitude: float


class Fleet(BaseModel):
    manager: str
    owner: str
    serviceCenter: str


class Driver(BaseModel):
    name: str
    sbu: str
    type: str
    status: str
    jobDesc: str
    persona: str = "driver"
    truck: Location | None = None
    trailer: Location | None = None
    vendor_services: list[str] = []


class ContextResponse(BaseModel):
    context: Driver


class DriverContext(BaseModel):
    driverID: str
    driverCompany: str
    driverName: str
    truckCompany: str
    truckNumber: str
    employerCode: str
    fleetManager: str
    fleetServiceCenter: str
    fleetOwner: str
    trainingCoordinator: str
    trainingCoordinatorSupervisor: str
    driverJobDesc: str
    stateZone: str
    orderNumber: int
    primaryCoverage: str
    driverSBU: str
    driverType: str
    orderSBU: str
    orderTerminal: str
    domicileTerminal: str
    driverStatus: str
    currentPTA: str
    truckLocation: str
    preferredLanguage: str


class DriverData(BaseModel):
    driverdata: DriverContext
