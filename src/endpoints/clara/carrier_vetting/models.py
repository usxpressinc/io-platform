from pydantic import BaseModel, EmailStr, Field


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


class CarrierContact(BaseModel):
    name: str | None = None
    emailAddresses: list[EmailStr] | str = []
    phones: list[str] = []
    isType: str | None = None


class CarrierValidityRequest(BaseModel):
    dotNumber: str | None = None
    mcNumber: str | None = None
    brokerageOrderId: str | None = None


class CarrierValidityError(BaseModel):
    code: str
    description: str


CARRIER_CONTACTS: list[CarrierContact] = []


def get_carrier_contacts():
    return CARRIER_CONTACTS


class CarrierValidityResponse(BaseModel):
    isValid: bool | str
    error: CarrierValidityError | None = None
    failedBy: list[str] = []
    statusCode: int | str = 200
    contacts: list[CarrierContact] = Field(default_factory=get_carrier_contacts)


schema = CarrierValidityResponse(
    isValid="""If true, then you're good to sell this load to the carrier by using the move_on tool.
If false, you cannot sell this load to this carrier,
meaning the carrier is not eligible to move this load and thus you cannot use the "move_on"
tool under any circumstances, instead when this field is "false" follow the description for next steps.""",
    error=CarrierValidityError(
        code="If not valid, then this tells you why the carrier failed the check",
        description="This gives more information about the code. This is what should be provided to the user",
    ),
    failedBy=[
        "This isn't useful for the user but it helps us understand which check the validity failed"
    ],
    statusCode="""If it is 400s, then it means that what the user provided has some issues.
If it is in 500s, then there is some network or application issue""".replace(
        "\n", " "
    ).replace(
        "\r", ""
    ),
    contacts=[
        CarrierContact(
            emailAddresses="""This contains list of email addresses of the carrier.
Use this list of to verify if the email address provided by the caller is here""".replace(
                "\n", " "
            ).replace(
                "\r", ""
            ),
            phones=["This contains list of phone numbers of the contact."],
            isType="This tells you about the role of the contact",
            name="Name of the contact",
        )
    ],
)
