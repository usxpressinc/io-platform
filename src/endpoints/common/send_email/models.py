from pydantic import BaseModel


class SendEmailResponse(BaseModel):
    status: str
    errors: list[str] = []


class SendEmailRequest(BaseModel):
    body: str
    from_email: str
    to_emails: str
    subject: str
    cc_emails: str = ""
    bcc_emails: str = ""
