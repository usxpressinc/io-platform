from pydantic import BaseModel, EmailStr


class DispatchContact(BaseModel):
    name: str
    phone: str
    email_address: str


class LineItemContact(BaseModel):
    is_type: str
    name: str
    email_address: str
    phone: str


class Contact(BaseModel):
    is_type: str
    name: str


class Phone(BaseModel):
    is_type: str
    value: str
    country_code: str
    country_code_prefix: str


class EmailAddress(BaseModel):
    is_type: str
    value: str


class Model(BaseModel):
    dispatch_contact: DispatchContact
    line_item_contacts: list[LineItemContact]
    contacts: list[Contact]
    phones: list[Phone]
    email_addresses: list[EmailAddress]


class CarrierContacts(BaseModel):
    name: str | None
    emailAddresses: list[EmailStr] = []
    phones: list[str] = []
    is_type: str | None


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
    contacts: list[CarrierContacts] | str = []


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
