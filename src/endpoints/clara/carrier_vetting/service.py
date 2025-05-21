import logging

import pydash
from fastapi import HTTPException, status
from glom import Match, glom

from src.prompts import ClaraPrompts
from src.settings import Settings

from . import constants, helpers, models

settings = Settings.model_validate({})
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
            if email_address and isinstance(my_item.emailAddresses, list):
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
            if email_address not in my_item.emailAddresses and isinstance(
                my_item.emailAddresses, list
            ):
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

    if highway_json["connection"] is None:
        return models.CarrierValidityResponse(
            isValid="false",
            errors=[errors.HighwaySetup],
            failedBy=["connection=null"],
        )

    # connection.status == "do_not_dispatch"
    if glom(highway_json, "connection.status", default="") == "do_not_dispatch":
        return models.CarrierValidityResponse(
            isValid="false",
            errors=[errors.DoNotUse],
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
            isValid="false",
            errors=[errors.HighwayConnect],
            failedBy=[
                "connection.status={}".format(
                    glom(highway_json, "connection.status", default="")
                )
            ],
        )

    # Check if connection.is_monitored == true
    if not glom(highway_json, "connection.is_monitored", default=False):
        logger.error("Setup Highway connection.is_monitored")
        return models.CarrierValidityResponse(
            isValid="false",
            errors=[errors.HighwaySetup],
            failedBy=[
                "connection.is_monitored={}".format(
                    glom(highway_json, "connection.is_monitored", default=True)
                )
            ],
        )

    # This also checks if carrier has `do_not_dispatch` rule
    mcleod_carrier = get_mcleod_carrier(highway_json=highway_json)

    # rules_assessment.overall_result == "pass"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "pass"
    ):
        return models.CarrierValidityResponse(isValid="true")

    # rules_assessment.overall_result == "incomplete"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "incomplete"
    ):
        return models.CarrierValidityResponse(
            isValid="false",
            errors=[errors.HighwayConnect],
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
        failed_data = failed_classifications(highway_json=highway_json)
        if failed_data is not None:
            return failed_data

        # Call the mcleod api "GET /carriers/checkQualification"
        checks = to_check_mcleod_qualification(highway_json=highway_json)
        if len(checks.errors) > 0:
            # helpers.check_mcleod_carrier_qualification(carrier_id=mcleod_carrier_json["id"], )
            mcleod_carrier_validity = get_mcleod_validity(
                mcleod_carrier=mcleod_carrier,
                brokerage_order_id=brokerage_order_id,
            )
            if not mcleod_carrier_validity:
                checks.failedBy.insert(
                    0, "rules_assessment.overall_result=fail"
                )
                return models.CarrierValidityResponse(
                    isValid="false",
                    errors=checks.errors,
                    failedBy=checks.failedBy,
                )

    # rules_assessment.overall_result == "partial_pass"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "partial_pass"
    ):
        mcleod_carrier_validity = get_mcleod_validity(
            mcleod_carrier=mcleod_carrier, brokerage_order_id=brokerage_order_id
        )
        if not mcleod_carrier_validity:
            return models.CarrierValidityResponse(
                isValid="false",
                errors=[errors.SellAltLoad],
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

    return models.CarrierValidityResponse(isValid="true")


def get_mcleod_validity(mcleod_carrier, brokerage_order_id: str | None) -> bool:
    order_details = helpers.get_mcleod_order(order_id=brokerage_order_id)
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
        if glom(mcleod_carrier, "drsPayee.no_dispatch", default=False):
            raise HTTPException(
                status_code=status.HTTP_200_OK,
                detail=models.CarrierValidityResponse(
                    isValid="false",
                    errors=[errors.DoNotUse],
                    failedBy=[
                        "drsPayee.no_dispatch={}".format(
                            glom(
                                mcleod_carrier,
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
                    isValid="false",
                    errors=[
                        models.CarrierValidityError(
                            code="multiple_mcleod_carrier",
                            description=prompts.multiple_mcleod_carrier,
                        )
                    ],
                    failedBy=[x["id"] for x in mcleod_carrier_json],
                    statusCode=status.HTTP_400_BAD_REQUEST,
                ),
            )
        else:
            raise HTTPException(
                status_code=status.HTTP_200_OK,
                detail=models.CarrierValidityResponse(
                    isValid="false",
                    errors=[
                        models.CarrierValidityError(
                            code="no_mcleod_active_carrier",
                            description=prompts.use_compliance_check,
                        )
                    ],
                    statusCode=status.HTTP_404_NOT_FOUND,
                ),
            )
    else:
        raise HTTPException(
            status_code=status.HTTP_200_OK,
            detail=models.CarrierValidityResponse(
                isValid="false",
                errors=[
                    models.CarrierValidityError(
                        code="no_mcleod_carrier",
                        description=prompts.use_compliance_check,
                    )
                ],
                statusCode=status.HTTP_404_NOT_FOUND,
            ),
        )
    return mcleod_carrier


def to_check_mcleod_qualification(
    highway_json,
) -> models.CarrierValidityResponse:
    response = models.CarrierValidityResponse(
        isValid="false",
        failedBy=[
            "rules_assessment.overall_result=fail",
        ],
    )
    response_errors = []
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
            response_errors.append(
                errors.CheckQualificationFailure(
                    item=constants.McleodCheckQualification.is_bond_surety_on_file
                )
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
            response_errors.append(
                errors.CheckQualificationFailure(
                    item=constants.McleodCheckQualification.phmsa_is_verified
                )
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
            response_errors.append(
                errors.CheckQualificationFailure(
                    item=constants.McleodCheckQualification.carb_acf
                )
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
            response_errors.append(
                errors.CheckQualificationFailure(
                    item=constants.McleodCheckQualification.carb_tru
                )
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
            response_errors.append(
                errors.CheckQualificationFailure(
                    item=constants.McleodCheckQualification.is_refrigeration_breakdown_included
                )
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
            response_errors.append(
                errors.CheckQualificationFailure(
                    item=constants.McleodCheckQualification.trailer_interchange
                )
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
    return response


def failed_classifications(
    highway_json,
) -> models.CarrierValidityResponse | None:
    failed_by = []
    print(
        [
            c
            for c in highway_json["rules_assessment"]["classifications"]
            if c.get("name") == "Interstate"
        ]
    )
    failed_errors: list[models.CarrierValidityError] = []
    for classification in highway_json["rules_assessment"]["classifications"]:
        name = classification.get("name")
        # Todo: Needs to be fixed
        if name not in ("Interstate", "Intrastate - US"):
            break
        failed_by = ["rules_assessment.overall_result=fail"]
        failed_assessments = []
        for rule, result in classification.get("rules", dict()).items():
            if result.get("result", "") != "pass":
                failed_assessments.append(rule)
        # failed_assessments = list(set(failed_assessments))

        if set(
            [
                "has_no_published_identity_alerts",
                "has_no_published_identity_theft_alerts",
            ]
        ).issubset(failed_assessments):
            failed_errors.append(errors.ComplianceCheck)
            failed_by.extend(
                [
                    "has_no_published_identity_alerts=fail",
                    "has_no_published_identity_theft_alerts=fail",
                ]
            )

        if set(["no_active_dispatcher_connections"]).issubset(
            failed_assessments
        ):
            failed_errors.append(errors.DispatchConnection)
            failed_by.extend(["no_active_dispatcher_connections=fail"])

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
            failed_errors.append(errors.DoNotUse)
            failed_by.extend(
                [
                    "authority_age_requirement=fail",
                    "has_dot_number=fail",
                    "has_verified_manually_entered_identifier=fail",
                    "is_authorized_for_property=fail",
                    "no_active_oos_orders=fail",
                    "safer_operating_status_active=fail",
                ]
            )

        if set(["no_recent_fmcsa_phone_or_email_change"]).issubset(
            failed_assessments
        ):
            failed_errors.append(errors.FmcsaContactChange)
            failed_by.extend(
                [
                    "no_recent_fmcsa_phone_or_email_change=fail",
                ]
            )

        if "new_jersey_auto_liability" in failed_assessments:
            failed_errors.append(
                errors.HighwayInsuranceUpdate(item="new_jersey_auto_liability")
            )
            failed_by.extend(
                [
                    "new_jersey_auto_liability=fail",
                ]
            )
        if "has_vin_if_auto_policy_is_scheduled_autos" in failed_assessments:
            failed_errors.append(
                errors.HighwayInsuranceUpdate(
                    item="has_vin_if_auto_policy_is_scheduled_autos"
                )
            )
            failed_by.extend(
                [
                    "has_vin_if_auto_policy_is_scheduled_autos=fail",
                ]
            )
        if "multi_currency_bipd_requirement" in failed_assessments:
            failed_errors.append(
                errors.HighwayInsuranceUpdate(
                    item="multi_currency_bipd_requirement"
                )
            )
            failed_by.extend(
                [
                    "multi_currency_bipd_requirement=fail",
                ]
            )
        if "multi_currency_cargo_requirement" in failed_assessments:
            failed_errors.append(
                errors.HighwayInsuranceUpdate(
                    item="multi_currency_cargo_requirement"
                )
            )
            failed_by.extend(
                [
                    "multi_currency_cargo_requirement=fail",
                ]
            )
        if "multi_currency_general_liability_requirement" in failed_assessments:
            failed_errors.append(
                errors.HighwayInsuranceUpdate(
                    item="multi_currency_general_liability_requirement"
                )
            )
            failed_by.extend(
                [
                    "multi_currency_general_liability_requirement=fail",
                ]
            )

        if set(
            [
                "has_verified_physical_location_activity",
                "has_eld_connected_and_active",
            ]
        ).issubset(failed_assessments):
            failed_errors.append(errors.TransferAgent)
            failed_by.extend(
                [
                    "has_verified_physical_location_activity=fail",
                    "has_eld_connected_and_active=fail",
                ]
            )
        rules = classification.get("rules", dict())
        for rule in [
            x
            for x in failed_assessments
            if "{}=fail".format(x) not in failed_by
        ]:
            failed_errors.append(
                models.CarrierValidityError(
                    code=rule, description=rules.get(rule).get("display_name")
                )
            )
            failed_by.append("{}=fail".format(rule))
        failed_errors = [
            models.CarrierValidityError(
                code=x.code, description=x.description, classification=name
            )
            for x in failed_errors
        ]
        failed_by = ["{}:{}".format(name, x) for x in failed_by]
        if name == "Interstate" and len(failed_errors) > 0:
            break
    if len(failed_errors) == 0:
        return None
    return models.CarrierValidityResponse(
        isValid="false",
        errors=failed_errors,
        failedBy=failed_by,
    )
