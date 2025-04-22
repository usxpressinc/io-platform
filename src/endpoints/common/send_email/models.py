from pydantic import BaseModel


class SendEmailResponse(BaseModel):
    status: str
    errors: list[str] = []


class SendEmailRequest(BaseModel):
    body: str
    from_email: str
    to_emails: list[str]
    subject: str
    cc_emails: list[str] = []
    bcc_emails: list[str] = []
