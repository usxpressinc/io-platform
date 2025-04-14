from pydantic import BaseModel


class FailedBy(BaseModel):
    fields: list[str] = []
    endpoint: str


class CarrierValidityRequest(BaseModel):
    dotNumber: str | None = None
    mcNumber: str | None = None
    brokerageOrderId: str | None = None


class CarrierValidityResponse(BaseModel):
    valid: bool = False
    errors: list[str] = []
    failedBy: FailedBy | None = None
