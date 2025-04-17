from pydantic import BaseModel


class CarrierValidityRequest(BaseModel):
    dotNumber: str | None = None
    mcNumber: str | None = None
    brokerageOrderId: str | None = None


class CarrierValidityResponse(BaseModel):
    isValid: bool | str
    error: str | None = None
    failedBy: list[str] = []
    statusCode: int | str = 200


schema = CarrierValidityResponse(
    isValid="If true, then the carrier is valid for the load",
    error="If not valid, then this tells you why the carrier failed",
    failedBy=[
        "This isn't useful for the user but it helps us understand which check the validity failed"
    ],
    statusCode="""If it is 400s, then it means that what the user provided has some issues.
        If it is in 500s, then there is some network or application issue""",
)
