from pydantic import BaseModel


class SendEmailResponse(BaseModel):
    status: str


class SendEmailRequest(BaseModel):
    body: str
    from_email: str
    to_email: str
    subject: str
