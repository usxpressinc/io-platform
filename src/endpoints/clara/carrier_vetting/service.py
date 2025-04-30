import logging

import pydash
from fastapi import HTTPException, status
from glom import Match, glom

from src.prompts import ClaraPrompts
from src.settings import Settings

from . import constants, helpers, models

settings = Settings()
errors = constants.Errors()
prompts = ClaraPrompts()
logger = logging.getLogger(__name__)


def get_highway_contacts(highway_json) -> list[models.CarrierContact]:
    contacts: list[models.CarrierContact] = []

    dispatch_contact = glom(
        highway_json, "contact_information.dispatch_contact", default=None
    )

    if dispatch_contact:
        name = dispatch_contact.get("name", "dispatch")
        phone = dispatch_contact.get("phone")
        email_address = dispatch_contact.get("email_address")
        c = models.CarrierContact(
            name=name,
            isType="dispatch",
        )
        if phone:
            c.phones = [phone]
        if email_address:
            c.emailAddresses = [email_address]
        contacts.append(c)

    line_item_contacts = glom(
        highway_json, "contact_information.line_item_contacts", default=[]
    )
    for contact in line_item_contacts:
        is_type = contact.get("is_type", "unknown")
        name = contact.get("name", is_type)
        phone = contact.get("phone")
        email_address = contact.get("email_address")

        my_item = next((item for item in contacts if item.name == name), None)

        if my_item:
            if phone:
                my_item.phones.append(phone)
                my_item.phones = list(set(my_item.phones))
            if email_address:
                my_item.emailAddresses.append(email_address)
                my_item.emailAddresses = list(set(my_item.emailAddresses))
        else:
            contacts.append(
                models.CarrierContact(
                    name=name,
                    phones=[phone],
                    emailAddresses=[email_address],
                    isType=is_type,
                )
            )

    h_contacts = glom(highway_json, "contact_information.contacts", default=[])
    for contact in h_contacts:
        is_type = contact.get("is_type", "unknown")
        name = contact.get("name", is_type)

        my_item = next(
            (
                item
                for item in contacts
                if item.isType == is_type and item.name == name
            ),
            None,
        )

        if not my_item:
            contacts.append(
                models.CarrierContact(
                    name=name,
                    isType=is_type,
                )
            )
    phones = glom(highway_json, "contact_information.phones", default=[])
    for contact in phones:
        is_type = contact.get("is_type", "unknown")
        phone = contact.get("value")
        country_code_prefix = contact.get("country_code_prefix", "+1")
        full_phone = f"{country_code_prefix} {phone}"

        my_item = next(
            (item for item in contacts if is_type == item.isType), None
        )

        if my_item:
            if phone in my_item.phones:
                my_item.phones = [
                    full_phone if phone == p else p for p in my_item.phones
                ]
            else:
                my_item.phones.append(full_phone)
        else:
            contacts.append(
                models.CarrierContact(
                    name=is_type,
                    phones=[full_phone],
                    isType=is_type,
                )
            )
    email_addresses = glom(
        highway_json, "contact_information.email_addresses", default=[]
    )
    for contact in email_addresses:
        is_type = contact.get("is_type", "unknown")
        email_address = contact.get("value")

        my_item = next(
            (item for item in contacts if is_type == item.isType), None
        )

        if my_item:
            if email_address not in my_item.emailAddresses:
                my_item.emailAddresses.append(email_address)

        else:
            contacts.append(
                models.CarrierContact(
                    name=is_type,
                    emailAddresses=[email_address],
                    isType=is_type,
                )
            )
    return contacts


def get_carrier_validity(
    dotNumber: str | None = None,
    mcNumber: str | None = None,
    brokerage_order_id: str | None = None,
) -> models.CarrierValidityResponse:
    """
    ## Check whether a carrier is valid using Highway API
    """
    highway_json = helpers.get_highway_details(
        dotNumber=dotNumber, mcNumber=mcNumber
    )

    models.CARRIER_CONTACTS = get_highway_contacts(highway_json=highway_json)

    # Check if connection.is_monitored == true
    if not glom(highway_json, "connection.is_monitored", default=True):
        logger.error("Setup Highway connection.is_monitored")
        return models.CarrierValidityResponse(
            isValid=False,
            error=errors.HighwaySetup,
            failedBy=[
                "connection.is_monitored={}".format(
                    glom(highway_json, "connection.is_monitored", default=True)
                )
            ],
        )

    # connection.status == "do_not_dispatch"
    if glom(highway_json, "connection.status", default="") == "do_not_dispatch":
        return models.CarrierValidityResponse(
            isValid=False,
            error=errors.DoNotUse,
            failedBy=[
                "connection.status={}".format(
                    glom(highway_json, "connection.status", default="")
                )
            ],
        )

    # connection.status == "needs_to_onboard"
    if (
        glom(highway_json, "connection.status", default="")
        == "needs_to_onboard"
    ):
        return models.CarrierValidityResponse(
            isValid=False,
            error=errors.HighwayConnect,
            failedBy=[
                "connection.status={}".format(
                    glom(highway_json, "connection.status", default="")
                )
            ],
            endpoint="highway",
        )
    # rules_assessment.overall_result == "pass"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "pass"
    ):
        return models.CarrierValidityResponse(isValid=True)

    # rules_assessment.overall_result == "incomplete"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "incomplete"
    ):
        return models.CarrierValidityResponse(
            isValid=False,
            error=errors.HighwayConnect,
            failedBy=[
                "rules_assessment.overall_result={}".format(
                    glom(
                        highway_json,
                        "rules_assessment.overall_result",
                        default="",
                    )
                )
            ],
        )

    # rules_assessment.overall_result == "fail"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "fail"
    ):
        failed_assessments = []
        for classification in glom(
            highway_json, "rules_assessment.classifications", default=[]
        ):
            for rule, result in classification.get("rules", dict()).items():
                if result.get("result", "") != "pass":
                    failed_assessments.append(rule)
        failed_assessments = list(set(failed_assessments))

        if set(
            [
                "has_no_published_identity_alerts",
                "has_no_published_identity_theft_alerts",
            ]
        ).issubset(failed_assessments):
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.ComplianceCheck,
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "has_no_published_identity_alerts=fail",
                    "has_no_published_identity_theft_alerts=fail",
                ],
            )

        if set(["no_active_dispatcher_connections"]).issubset(
            failed_assessments
        ):
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.DispatchConnection,
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "no_active_dispatcher_connections=fail",
                ],
            )

        if (
            glom(
                highway_json,
                "authority_assessment.carrier_interstate_authority_check",
                default="",
            ).lower()
            != "active"
            or glom(
                highway_json,
                "authority_assessment.latest_safety_rating",
                default="",
            ).lower()
            not in ["satisfactory", "unrated"]
            or (
                glom(
                    highway_json,
                    "authority_assessment.carrier_interstate_authority_check",
                    default="",
                ).lower()
                != "active"
            )
        ):
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.DoNotUse,
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "authority_assessment.carrier_interstate_authority_check={}".format(
                        glom(
                            highway_json,
                            "authority_assessment.carrier_interstate_authority_check",
                            default="",
                        )
                    ),
                    "authority_assessment.latest_safety_rating={}".format(
                        glom(
                            highway_json,
                            "authority_assessment.latest_safety_rating",
                            default="",
                        )
                    ),
                ],
            )

        if set(
            [
                "authority_age_requirement",
                "has_dot_number",
                "has_verified_manually_entered_identifier",
                "is_authorized_for_property",
                "no_active_oos_orders",
                "safer_operating_status_active",
            ]
        ).issubset(failed_assessments):
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.DoNotUse,
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "authority_age_requirement=fail",
                    "has_dot_number=fail",
                    "has_verified_manually_entered_identifier=fail",
                    "is_authorized_for_property=fail",
                    "no_active_oos_orders=fail",
                    "safer_operating_status_active=fail",
                ],
            )

        if set(["no_recent_fmcsa_phone_or_email_change"]).issubset(
            failed_assessments
        ):
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.FmcsaContactChange,
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "no_recent_fmcsa_phone_or_email_change=fail",
                ],
            )

        if "new_jersey_auto_liability" in failed_assessments:
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.HighwayInsuranceUpdate(
                    item="new_jersey_auto_liability"
                ),
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "new_jersey_auto_liability=fail",
                ],
            )
        if "has_vin_if_auto_policy_is_scheduled_autos" in failed_assessments:
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.HighwayInsuranceUpdate(
                    item="has_vin_if_auto_policy_is_scheduled_autos"
                ),
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "has_vin_if_auto_policy_is_scheduled_autos=fail",
                ],
            )
        if "multi_currency_bipd_requirement" in failed_assessments:
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.HighwayInsuranceUpdate(
                    item="multi_currency_bipd_requirement"
                ),
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "multi_currency_bipd_requirement=fail",
                ],
            )
        if "multi_currency_cargo_requirement" in failed_assessments:
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.HighwayInsuranceUpdate(
                    item="multi_currency_cargo_requirement"
                ),
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "multi_currency_cargo_requirement=fail",
                ],
            )
        if "multi_currency_general_liability_requirement" in failed_assessments:
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.HighwayInsuranceUpdate(
                    item="multi_currency_general_liability_requirement"
                ),
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "multi_currency_general_liability_requirement=fail",
                ],
            )

        if set(
            [
                "has_verified_physical_location_activity",
                "has_eld_connected_and_active",
            ]
        ).issubset(failed_assessments):
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.TransferAgent,
                failedBy=[
                    "rules_assessment.overall_result=fail",
                    "has_verified_physical_location_activity=fail",
                    "has_eld_connected_and_active=fail",
                ],
            )

        # Call the mcleod api "GET /carriers/checkQualification"
        err = to_check_mcleod_qualification(highway_json=highway_json)
        if err:
            # helpers.check_mcleod_carrier_qualification(carrier_id=mcleod_carrier_json["id"], )
            mcleod_carrier_validity = get_mcleod_validity(
                highway_json=highway_json, brokerage_order_id=brokerage_order_id
            )
            if not mcleod_carrier_validity:
                return models.CarrierValidityResponse(
                    isValid=False,
                    error=err,
                    failedBy=[
                        "rules_assessment.overall_result=fail",
                        "authority.latest_li_authority.is_bond_surety_on_file",
                        "certifications.all",
                        "certifications.verified.carb_acf",
                        "certifications.verified.carb_tru",
                        "insurance.insurance_policies[is_type=motor_truck_cargo].is_refrigeration_breakdown_included",
                        "insurance.insurance_policies[is_type=trailer_interchange].limit",
                    ],
                )

    # rules_assessment.overall_result == "partial_pass"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "partial_pass"
    ):
        mcleod_carrier_validity = get_mcleod_validity(
            highway_json=highway_json, brokerage_order_id=brokerage_order_id
        )
        if not mcleod_carrier_validity:
            return models.CarrierValidityResponse(
                isValid=False,
                error=errors.SellAltLoad,
                failedBy=[
                    "rules_assessment.overall_result={}".format(
                        glom(
                            highway_json,
                            "rules_assessment.overall_result",
                            default="",
                        )
                    ),
                    "check_mcleod_carrier_qualification={}".format(
                        mcleod_carrier_validity
                    ),
                ],
            )

    return models.CarrierValidityResponse(isValid=True)


def get_mcleod_validity(highway_json, brokerage_order_id: str) -> bool:
    order_details = helpers.get_mcleod_order(order_id=brokerage_order_id)
    mcleod_carrier = get_mcleod_carrier(highway_json=highway_json)
    return helpers.check_mcleod_carrier_qualification(
        carrier_id=mcleod_carrier["id"],
        movement=order_details["curr_movement_id"],
    )


def get_mcleod_carrier(highway_json):
    # Mcleod do_not_dispatch == true
    mcleod_carrier_json = helpers.get_mcleod_carrier_details(
        dotNumber=int(pydash.get(highway_json, "dot_number") or 0),
        mcNumber=int(pydash.get(highway_json, "mc_number") or 0),
    )
    mcleod_carrier = dict()
    if len(mcleod_carrier_json) == 1:
        mcleod_carrier = mcleod_carrier_json[0]
        if glom(mcleod_carrier_json, "drsPayee.no_dispatch", default=False):
            raise HTTPException(
                status_code=status.HTTP_200_OK,
                detail=models.CarrierValidityResponse(
                    isValid=False,
                    error=errors.DoNotUse,
                    failedBy=[
                        "drsPayee.no_dispatch={}".format(
                            glom(
                                mcleod_carrier_json,
                                "drsPayee.no_dispatch",
                                default=False,
                            )
                        )
                    ],
                ),
            )
    elif len(mcleod_carrier_json) > 1:
        mcleod_carrier_json = [
            x for x in mcleod_carrier_json if x["status"] == "A"
        ]
        if len(mcleod_carrier_json) == 1:
            mcleod_carrier = mcleod_carrier_json[0]
        elif len(mcleod_carrier_json) > 1:
            raise HTTPException(
                status_code=status.HTTP_200_OK,
                detail=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="multiple_mcleod_carrier",
                        description=prompts.MultipleMcleodCarrier,
                    ),
                    failedBy=[x["id"] for x in mcleod_carrier_json],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
            )
        else:
            raise HTTPException(
                status_code=status.HTTP_200_OK,
                detail=models.CarrierValidityResponse(
                    isValid=False,
                    error=models.CarrierValidityError(
                        code="no_mcleod_active_carrier",
                        description=prompts.UseComplianceCheck,
                    ),
                    statusCode=status.HTTP_404_NOT_FOUND,
                ),
            )
    else:
        raise HTTPException(
            status_code=status.HTTP_200_OK,
            detail=models.CarrierValidityResponse(
                isValid=False,
                error=models.CarrierValidityError(
                    code="no_mcleod_carrier",
                    description=prompts.UseComplianceCheck,
                ),
                statusCode=status.HTTP_404_NOT_FOUND,
            ),
        )
    return mcleod_carrier


def to_check_mcleod_qualification(highway_json) -> models.CarrierValidityError:
    response = models.CarrierValidityResponse(
        isValid=False,
        failedBy=[
            "rules_assessment.overall_result=fail",
        ],
    )
    if (
        not glom(
            highway_json,
            "authority.latest_li_authority.is_bond_surety_on_file",
            default=False,
        )
        or not glom(
            highway_json,
            (
                "certifications.all",
                Match({"is_type": "phmsa"}),
                "is_verified",
            ),
            default=False,
        )
        or not glom(
            highway_json, "certifications.verified.carb_acf", default=False
        )
        or not glom(
            highway_json, "certifications.verified.carb_tru", default=False
        )
        or not glom(
            highway_json,
            (
                "insurance.insurance_policies",
                Match({"is_type": "motor_truck_cargo"}),
                "is_refrigeration_breakdown_included",
            ),
            default=False,
        )
        or glom(
            highway_json,
            (
                "insurance.insurance_policies",
                Match({"is_type": "trailer_interchange"}),
                "limit",
            ),
            default="",
        )
        != "pass"
    ):
        if not glom(
            highway_json,
            "authority.latest_li_authority.is_bond_surety_on_file",
            default=False,
        ):
            response.error = errors.CheckQualificationFailure(
                item=constants.McleodCheckQualification.is_bond_surety_on_file
            )
            response.failedBy.append(
                "is_bond_surety_on_file={}".format(
                    glom(
                        highway_json,
                        "authority.latest_li_authority.is_bond_surety_on_file",
                        default=False,
                    )
                )
            )
        if not glom(
            highway_json,
            (
                "certifications.all",
                Match({"is_type": "phmsa"}),
                "is_verified",
            ),
            default=False,
        ):
            response.error = errors.CheckQualificationFailure(
                item=constants.McleodCheckQualification.phmsa_is_verified
            )
            response.failedBy.append(
                "certifications.all.phmsa.is_verified={}".format(
                    glom(
                        highway_json,
                        (
                            "certifications.all",
                            Match({"is_type": "phmsa"}),
                            "is_verified",
                        ),
                        default=False,
                    ),
                )
            )

        if not glom(
            highway_json, "certifications.verified.carb_acf", default=False
        ):
            response.error = errors.CheckQualificationFailure(
                item=constants.McleodCheckQualification.carb_acf
            )
            response.failedBy.append(
                "certifications.verified.carb_acf={}".format(
                    glom(
                        highway_json,
                        "certifications.verified.carb_acf",
                        default=False,
                    )
                )
            )

        if not glom(
            highway_json, "certifications.verified.carb_tru", default=False
        ):
            response.error = errors.CheckQualificationFailure(
                item=constants.McleodCheckQualification.carb_tru
            )
            response.failedBy.append(
                "certifications.verified.carb_tru={}".format(
                    glom(
                        highway_json,
                        "certifications.verified.carb_tru",
                        default=False,
                    )
                )
            )
        if not glom(
            highway_json,
            (
                "insurance.insurance_policies",
                Match({"is_type": "motor_truck_cargo"}),
                "is_refrigeration_breakdown_included",
            ),
            default=False,
        ):
            response.error = errors.CheckQualificationFailure(
                item=constants.McleodCheckQualification.is_refrigeration_breakdown_included
            )
            response.failedBy.append(
                "insurance.insurance_policies.motor_truck_cargo.is_refrigeration_breakdown_included={}".format(
                    glom(
                        highway_json,
                        (
                            "insurance.insurance_policies",
                            Match({"is_type": "motor_truck_cargo"}),
                            "is_refrigeration_breakdown_included",
                        ),
                        default=False,
                    )
                )
            )
        if glom(
            highway_json,
            (
                "insurance.insurance_policies",
                Match({"is_type": "trailer_interchange"}),
                "limit",
            ),
            default="",
        ):
            response.error = errors.CheckQualificationFailure(
                item=constants.McleodCheckQualification.trailer_interchange
            )
            response.failedBy.append(
                "insurance.insurance_policies.trailer_interchange.limit={}".format(
                    glom(
                        highway_json,
                        (
                            "insurance.insurance_policies",
                            Match({"is_type": "trailer_interchange"}),
                            "limit",
                        ),
                        default="",
                    )
                )
            )
    return None
