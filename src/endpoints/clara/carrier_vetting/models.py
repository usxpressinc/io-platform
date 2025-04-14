from pydantic import BaseModel


class CarrierValidityResponse(BaseModel):
    valid: bool = False
    errors: list[str] = []


class CarrierValidityRequest(BaseModel):
    dotNumber: int | None = None
    mcNumber: int | None = None
    movementNumber: int | None = None
