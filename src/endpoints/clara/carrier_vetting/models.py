from pydantic import BaseModel


class CarrierValidityRequest(BaseModel):
    dotNumber: str | None = None
    mcNumber: str | None = None
    brokerageOrderId: str | None = None


class CarrierValidityError(BaseModel):
    code: str
    description: str


class CarrierValidityResponse(BaseModel):
    isValid: bool | str
    error: CarrierValidityError | None = None
    failedBy: list[str] = []
    statusCode: int | str = 200


schema = CarrierValidityResponse(
    isValid="If true, then you're good to sell this load to the carrier. Please use the 'move_on' tool.",
    error=CarrierValidityError(
        code="If not valid, then this tells you why the carrier failed the check",
        description="This gives more information about the code. This is what should be provided to the user",
    ),
    failedBy=[
        "This isn't useful for the user but it helps us understand which check the validity failed"
    ],
    statusCode="""If it is 400s, then it means that what the user provided has some issues.
        If it is in 500s, then there is some network or application issue""",
)
