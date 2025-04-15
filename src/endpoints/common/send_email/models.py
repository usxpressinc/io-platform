from pydantic import BaseModel, EmailStr


class SendEmailResponse(BaseModel):
    status: str


class SendEmailRequest(BaseModel):
    body: str
    from_email: EmailStr
    to_email: EmailStr
    subject: str
