import logging
import os
import typing

import sendgrid
from glom import glom
from jinja2 import Environment, FileSystemLoader, select_autoescape
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


BASE_DIR = os.path.dirname(__file__)
env = Environment(
    loader=FileSystemLoader(searchpath=BASE_DIR),
    autoescape=select_autoescape(["html", "xml"]),
)
template = env.get_template("signature_template.jinja")


def render_signature(content: models.SendGridBody):
    """
    user: dict with keys 'full_name', 'email', 'logo_url', and optionally 'title'
    """
    return template.render(
        full_name=content.name,
        email=content.email,
        logo_url=content.logo_url,
        title=content.title,
        content=content.content,
    )


def send_email(item: models.SendEmailRequest) -> models.SendEmailResponse:
    sg = sendgrid.SendGridAPIClient(api_key=settings.Email_SendgridKey)

    logger.info(repr(item))

    from_email = Email(item.from_email)
    subject = item.subject

    from_name = item.from_name
    if from_name is None:
        from_name = from_email.name
        if from_email.name is None:
            from_name = "USXpress"

    sendgrid_user = models.SendGridBody(
        name=typing.cast(str, from_name),
        title=item.title,
        email=typing.cast(str, from_email.email),
        logo_url=settings.Email_SignatureLogoUrl,
        content=item.body,
    )
    html_content = render_signature(sendgrid_user)

    logger.info(html_content)

    email = Mail(
        from_email=from_email,
        subject=subject,
        html_content=HtmlContent(html_content),
    )

    personalization = Personalization()
    for to_email in item.to_emails.split(";"):
        personalization.add_to(To(to_email))
    if item.cc_emails:
        for cc_email in item.cc_emails.split(";"):
            personalization.add_cc(Cc(cc_email))
    if item.bcc_emails:
        for bcc_email in item.bcc_emails.split(";"):
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
