from pydantic import BaseModel


class ScheduledAppointmentTimestamp(BaseModel):
    early: str
    late: str


class Location(BaseModel):
    city: str
    state: str
    zip: str


class Pickup(BaseModel):
    scheduledAppointmentTimestamp: ScheduledAppointmentTimestamp
    location: Location


class Delivery(BaseModel):
    scheduledAppointmentTimestamp: ScheduledAppointmentTimestamp
    location: Location


class Load(BaseModel):
    isBrokerage: str
    isAsset: str
    company: str
    customerName: str
    freightId: str
    pickup: Pickup
    delivery: Delivery
    distance: str
    equipmentType: str
    isHazmat: str
    isTeamRequired: str
    isTempProtectRequired: str
    carrierCode: str


class Email(BaseModel):
    receivedTimestamp: str
    senderAddress: str


class Assumption(BaseModel):
    field: str
    assumption: str
    reason: str


class Meta(BaseModel):
    email: Email
    specialInstructions: str
    assumptions: list[Assumption]


class Request(BaseModel):
    load: Load
    meta: Meta
