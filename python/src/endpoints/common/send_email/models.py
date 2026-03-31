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
    title: str | None = None
    from_name: str | None = None


class SendGridBody(BaseModel):
    name: str
    title: str | None = None
    email: str
    logo_url: str
    content: str
