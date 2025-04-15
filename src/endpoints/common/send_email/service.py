import logging

import sendgrid
from sendgrid.helpers.mail import Email, HtmlContent, Mail, To

from src.settings import Settings

from . import models

settings = Settings()
logger = logging.getLogger(__name__)


def send_email(item: models.SendEmailRequest) -> models.SendEmailResponse:
    sg = sendgrid.SendGridAPIClient(api_key=settings.Email_SendgridKey)

    from_email = Email(item.from_email)
    to_email = To(item.to_email)
    subject = item.subject
    content = HtmlContent(item.body)
    mail = Mail(from_email, to_email, subject, content)
    response = sg.client.mail.send.post(request_body=mail.get())
    logger.info(response.status_code)
    logger.info(response.body)
    logger.info(response.headers)
    return models.SendEmailResponse(status=str(response.status_code))
