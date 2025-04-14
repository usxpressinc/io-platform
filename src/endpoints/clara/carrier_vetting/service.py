import logging

from fastapi import HTTPException, status
from glom import Match, glom

from src.settings import Settings

from . import constants, helpers, models

settings = Settings()
errors = constants.Errors()
logger = logging.getLogger(__name__)


def get_carrier_validity(
    dotNumber: int | None = None,
    mcNumber: int | None = None,
    movementNumber: int | None = None,
) -> models.CarrierValidityResponse:
    """
    ## Check whether a carrier is valid using Highway API
    """
    highway_json = helpers.get_highway_details(
        dotNumber=dotNumber, mcNumber=mcNumber
    )

    # Check if connection.is_monitored == true
    if not glom(highway_json, "connection.is_monitored", default=False):
        logger.error("Setup Highway connection.is_monitored")
        return models.CarrierValidityResponse(
            valid=False, errors=[errors.HighwaySetup]
        )

    # connection.status == "do_not_dispatch"
    if glom(highway_json, "connection.status", default="") == "do_not_dispatch":
        return models.CarrierValidityResponse(
            valid=False, errors=[errors.DoNotUse]
        )

    # connection.status == "needs_to_onboard"
    if (
        glom(highway_json, "connection.status", default="")
        == "needs_to_onboard"
    ):
        return models.CarrierValidityResponse(
            valid=False, errors=[errors.HighwayConnect]
        )

    # connection.status == "needs_to_onboard"
    if (
        glom(highway_json, "connection.status", default="")
        == "needs_to_onboard"
    ):
        return models.CarrierValidityResponse(
            valid=False, errors=[errors.HighwayConnect]
        )

    # Mcleod do_not_dispatch == true
    mcleod_carrier_json = helpers.get_mcleod_carrier_details(
        dotNumber=dotNumber, mcNumber=mcNumber
    )
    if len(mcleod_carrier_json) == 1:
        if (mcleod_carrier_json[0].get("drsPayee", dict())).get(
            "no_dispatch", True
        ):
            logger.warning("Mcleod no_dispatch %s", (mcleod_carrier_json[0].get("drsPayee", dict())).get(
            "no_dispatch", True
        ))
            return models.CarrierValidityResponse(
                valid=False, errors=[errors.DoNotUse]
            )
    elif len(mcleod_carrier_json) > 1:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                valid=False,
                errors=[errors.McleodManyCarrier].extend(
                    [x["id"] for x in mcleod_carrier_json]
                ),
            ).model_dump(),
        )
    else:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=models.CarrierValidityResponse(
                valid=False, errors=[errors.McleodNoCarrier]
            ).model_dump(),
        )

    # rules_assessment.overall_result == "pass"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "pass"
    ):
        return models.CarrierValidityResponse(valid=True, errors=[])

    # rules_assessment.overall_result == "incomplete"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "incomplete"
    ):
        return models.CarrierValidityResponse(
            valid=False, errors=[errors.HighwaySetup]
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
            for rule, result in classification.get("rules", []):
                if result.get("result", "") != "pass":
                    failed_assessments.append(rule)
        failed_assessments = list(set(failed_assessments))
        responseErrors = []

        if set(
            [
                "has_no_published_identity_alerts",
                "has_no_published_identity_theft_alerts",
            ]
        ).issubset(failed_assessments):
            responseErrors.append(errors.ComplianceCheck)

        if set(["no_active_dispatcher_connections"]).issubset(
            failed_assessments
        ):
            responseErrors.append(errors.DispatchConnection)

        if (
            glom(
                highway_json,
                "authority_assessment.carrier_interstate_authority_check",
                default="",
            ).lower()
            != "active"
        ):
            responseErrors.append(errors.DoNotUse)
        if glom(
            highway_json,
            "authority_assessment.latest_safety_rating",
            default="",
        ).lower() not in ["satisfactory", "unrated"]:
            responseErrors.append(errors.DoNotUse)
        if (
            glom(
                highway_json,
                "authority_assessment.carrier_interstate_authority_check",
                default="",
            ).lower()
            != "active"
        ):
            responseErrors.append(errors.DoNotUse)

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
            responseErrors.append(errors.DispatchConnection)

        if set(["no_recent_fmcsa_phone_or_email_change"]).issubset(
            failed_assessments
        ):
            responseErrors.append(errors.FmcsaContactChange)

        if set(
            [
                "new_jersey_auto_liability",
                "has_vin_if_auto_policy_is_scheduled_autos",
                "multi_currency_bipd_requirement",
                "multi_currency_cargo_requirement",
                "multi_currency_general_liability_requirement",
            ]
        ).issubset(failed_assessments):
            responseErrors.append(errors.HighwayInsuranceUpdate)

        if set(
            [
                "has_verified_physical_location_activity",
                "has_eld_connected_and_active",
            ]
        ).issubset(failed_assessments):
            responseErrors.append(errors.TransferAgent)

        # Call the mcleod api "GET /carriers/checkQualification"
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
            # helpers.check_mcleod_carrier_qualification(carrier_id=mcleod_carrier_json["id"], )
            responseErrors.append(errors.SellAltLoad)
        return models.CarrierValidityResponse(
            valid=False, errors=[errors.HighwaySetup]
        )

    return models.CarrierValidityResponse(valid=False)
