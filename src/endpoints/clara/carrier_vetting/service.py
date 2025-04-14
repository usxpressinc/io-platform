import logging

from fastapi import HTTPException, status
from glom import Match, glom

from src.settings import Settings

from . import constants, helpers, models

settings = Settings()
errors = constants.Errors()
logger = logging.getLogger(__name__)


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

    # Check if connection.is_monitored == true
    if not glom(highway_json, "connection.is_monitored", default=True):
        logger.error("Setup Highway connection.is_monitored")
        return models.CarrierValidityResponse(
            valid=False,
            errors=[errors.HighwaySetup],
            failedBy=models.FailedBy(
                fields=["connection.is_monitored"], endpoint="highway"
            ),
        )

    # connection.status == "do_not_dispatch"
    if glom(highway_json, "connection.status", default="") == "do_not_dispatch":
        return models.CarrierValidityResponse(
            valid=False,
            errors=[errors.DoNotUse],
            failedBy=models.FailedBy(
                fields=["connection.status"], endpoint="highway"
            ),
        )

    # connection.status == "needs_to_onboard"
    if (
        glom(highway_json, "connection.status", default="")
        == "needs_to_onboard"
    ):
        return models.CarrierValidityResponse(
            valid=False,
            errors=[errors.HighwayConnect],
            failedBy=models.FailedBy(
                fields=["connection.status"], endpoint="highway"
            ),
        )

    # Mcleod do_not_dispatch == true
    mcleod_carrier_json = helpers.get_mcleod_carrier_details(
        dotNumber=dotNumber, mcNumber=mcNumber
    )
    if len(mcleod_carrier_json) == 1:
        if glom(mcleod_carrier_json[0], "drsPayee.no_dispatch", default=False):
            return models.CarrierValidityResponse(
                valid=False,
                errors=[errors.DoNotUse],
                failedBy=models.FailedBy(
                    fields=["drsPayee.no_dispatch"], endpoint="mcleod"
                ),
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

    # rules_assessment.overall_result == "partial_pass"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "partial_pass"
    ):
        order_details = helpers.get_mcleod_order(order_id=brokerage_order_id)
        mcleod_carrier_validity = helpers.check_mcleod_carrier_qualification(
            carrier_id=mcleod_carrier_json[0]["id"],
            movement=order_details["curr_movement_id"],
        )
        if not mcleod_carrier_validity:
            return models.CarrierValidityResponse(
                valid=False,
                errors=[errors.SellAltLoad],
                failedBy=models.FailedBy(
                    fields=[
                        "rules_assessment.overall_result",
                    ],
                    endpoint="highway",
                ),
            )

    # rules_assessment.overall_result == "incomplete"
    if (
        glom(highway_json, "rules_assessment.overall_result", default="")
        == "incomplete"
    ):
        return models.CarrierValidityResponse(
            valid=False,
            errors=[errors.HighwaySetup],
            failedBy=models.FailedBy(
                fields=["rules_assessment.overall_result"], endpoint="highway"
            ),
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
                valid=False,
                errors=[errors.ComplianceCheck],
                failedBy=models.FailedBy(
                    fields=[
                        "has_no_published_identity_alerts",
                        "has_no_published_identity_theft_alerts",
                    ],
                    endpoint="highway",
                ),
            )

        if set(["no_active_dispatcher_connections"]).issubset(
            failed_assessments
        ):
            return models.CarrierValidityResponse(
                valid=False,
                errors=[errors.DispatchConnection],
                failedBy=models.FailedBy(
                    fields=[
                        "no_active_dispatcher_connections",
                    ],
                    endpoint="highway",
                ),
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
                valid=False,
                errors=[errors.DoNotUse],
                failedBy=models.FailedBy(
                    fields=[
                        "authority_assessment.carrier_interstate_authority_check",
                        "authority_assessment.latest_safety_rating",
                        "authority_assessment.carrier_interstate_authority_check",
                    ],
                    endpoint="highway",
                ),
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
                valid=False,
                errors=[errors.DispatchConnection],
                failedBy=models.FailedBy(
                    fields=[
                        "authority_age_requirement",
                        "has_dot_number",
                        "has_verified_manually_entered_identifier",
                        "is_authorized_for_property",
                        "no_active_oos_orders",
                        "safer_operating_status_active",
                    ],
                    endpoint="highway",
                ),
            )

        if set(["no_recent_fmcsa_phone_or_email_change"]).issubset(
            failed_assessments
        ):
            return models.CarrierValidityResponse(
                valid=False,
                errors=[errors.FmcsaContactChange],
                failedBy=models.FailedBy(
                    fields=["no_recent_fmcsa_phone_or_email_change"],
                    endpoint="highway",
                ),
            )

        if set(
            [
                "new_jersey_auto_liability",
                "has_vin_if_auto_policy_is_scheduled_autos",
                "multi_currency_bipd_requirement",
                "multi_currency_cargo_requirement",
                "multi_currency_general_liability_requirement",
            ]
        ).issubset(failed_assessments):
            return models.CarrierValidityResponse(
                valid=False,
                errors=[errors.HighwayInsuranceUpdate],
                failedBy=models.FailedBy(
                    fields=[
                        "new_jersey_auto_liability",
                        "has_vin_if_auto_policy_is_scheduled_autos",
                        "multi_currency_bipd_requirement",
                        "multi_currency_cargo_requirement",
                        "multi_currency_general_liability_requirement",
                    ],
                    endpoint="highway",
                ),
            )

        if set(
            [
                "has_verified_physical_location_activity",
                "has_eld_connected_and_active",
            ]
        ).issubset(failed_assessments):
            return models.CarrierValidityResponse(
                valid=False,
                errors=[errors.TransferAgent],
                failedBy=models.FailedBy(
                    fields=[
                        "has_verified_physical_location_activity",
                        "has_eld_connected_and_active",
                    ],
                    endpoint="highway",
                ),
            )

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
            order_details = helpers.get_mcleod_order(
                order_id=brokerage_order_id
            )
            mcleod_carrier_validity = (
                helpers.check_mcleod_carrier_qualification(
                    carrier_id=mcleod_carrier_json[0]["id"],
                    movement=order_details["curr_movement_id"],
                )
            )
            if not mcleod_carrier_validity:
                return models.CarrierValidityResponse(
                    valid=False,
                    errors=[errors.SellAltLoad],
                    failedBy=models.FailedBy(
                        fields=[
                            "authority.latest_li_authority.is_bond_surety_on_file",
                            "certifications.all",
                            "certifications.verified.carb_acf",
                            "certifications.verified.carb_tru",
                            "insurance.insurance_policies[is_type=motor_truck_cargo].is_refrigeration_breakdown_included",
                            "insurance.insurance_policies[is_type=trailer_interchange].limit",
                        ],
                        endpoint="highway",
                    ),
                )

    return models.CarrierValidityResponse(valid=True)
