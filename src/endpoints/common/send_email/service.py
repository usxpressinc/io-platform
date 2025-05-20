import logging

import sendgrid
from glom import glom
from python_http_client.exceptions import HTTPError
from sendgrid.helpers.mail import (
    Bcc,
    Cc,
    Email,
    HtmlContent,
    Mail,
    Personalization,
    To,
)

from src.settings import Settings

from . import models

settings = Settings.model_validate({})
logger = logging.getLogger(__name__)


def send_email(item: models.SendEmailRequest) -> models.SendEmailResponse:
    sg = sendgrid.SendGridAPIClient(api_key=settings.Email_SendgridKey)

    logger.info(repr(item))

    from_email = Email(item.from_email)
    subject = item.subject
    content = HtmlContent(item.body)
    email = Mail(from_email=from_email, subject=subject, html_content=content)

    personalization = Personalization()
    for to_email in item.to_emails:
        personalization.add_to(To(to_email))
    for cc_email in item.cc_emails:
        personalization.add_cc(Cc(cc_email))
    for bcc_email in item.bcc_emails:
        personalization.add_bcc(Bcc(bcc_email))
    email.add_personalization(personalization=personalization)

    try:
        response = sg.send(email)
        logger.info(response.status_code)
        logger.info(response.body)
        logger.info(response.headers)
    except HTTPError as e:
        logger.error(e.status_code)
        logger.error(e.body)
        logger.error(e.headers)
        return models.SendEmailResponse(
            status=str(e.status_code),
            errors=glom(e.to_dict.get("errors"), ["message"], default=[]),
        )
    return models.SendEmailResponse(status=str(response.status_code))
