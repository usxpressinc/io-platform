from src.prompts import ClaraPrompts

from .models import CarrierValidityError

prompts = ClaraPrompts.model_validate({})


class HighwayInsuranceUpdate:
    multi_currency_bipd_requirement = "multi_currency_bipd_requirement"
    new_jersey_auto_liability = "new_jersey_auto_liability"
    multi_currency_cargo_requirement = "multi_currency_cargo_requirement"
    multi_currency_general_liability_requirement = (
        "multi_currency_general_liability_requirement"
    )
    has_vin_if_auto_policy_is_scheduled_autos = (
        "has_vin_if_auto_policy_is_scheduled_autos"
    )

    @staticmethod
    def description(item: str):
        desc = ""
        match item:
            case HighwayInsuranceUpdate.multi_currency_bipd_requirement:
                desc = prompts.multi_currency_bipd_requirement
            case HighwayInsuranceUpdate.new_jersey_auto_liability:
                desc = prompts.new_jersey_auto_liability
            case HighwayInsuranceUpdate.multi_currency_cargo_requirement:
                desc = prompts.multi_currency_cargo_requirement
            case (
                HighwayInsuranceUpdate.multi_currency_general_liability_requirement
            ):
                desc = prompts.multi_currency_general_liability_requirement
            case (
                HighwayInsuranceUpdate.has_vin_if_auto_policy_is_scheduled_autos
            ):
                desc = prompts.has_vin_if_auto_policy_is_scheduled_autos
        return desc


class McleodCheckQualification:
    interstate_carrier_authority_is_active = (
        "interstate_carrier_authority_is_active"
    )
    is_refrigeration_breakdown_included = "is_refrigeration_breakdown_included"
    trailer_interchange = "trailer_interchange"
    carb_acf = "carb_acf"
    carb_tru = "carb_tru"
    is_bond_surety_on_file = "is_bond_surety_on_file"
    phmsa_is_verified = "phmsa_is_verified"

    @staticmethod
    def description(item: str):
        match item:
            case McleodCheckQualification.is_bond_surety_on_file:
                return prompts.is_bond_surety_on_file
            case McleodCheckQualification.phmsa_is_verified:
                return prompts.phmsa_is_verified
            case (
                McleodCheckQualification.interstate_carrier_authority_is_active
            ):
                return prompts.interstate_carrier_authority_is_active
            case McleodCheckQualification.is_refrigeration_breakdown_included:
                return prompts.is_refrigeration_breakdown_included
            case McleodCheckQualification.trailer_interchange:
                return prompts.trailer_interchange
            case McleodCheckQualification.carb_acf:
                return prompts.carb_acf
            case McleodCheckQualification.carb_tru:
                return prompts.carb_tru
            case _:
                return ""


class Errors:
    HighwaySetup = CarrierValidityError(
        code="highway_setup",
        description=prompts.highway_setup,
    )

    DoNotUse = CarrierValidityError(
        code="do_not_use",
        description=prompts.do_not_use,
    )

    HighwayConnect = CarrierValidityError(
        code="highway_connect",
        description=prompts.highway_connect,
    )

    ComplianceCheck = CarrierValidityError(
        code="compliance_check",
        description=prompts.compliance_check,
    )
    DispatchConnection = CarrierValidityError(
        code="dispatch_connection",
        description=prompts.dispatch_connection,
    )
    TransferAgent = CarrierValidityError(
        code="transfer_agent",
        description=prompts.transfer_agent,
    )
    FmcsaContactChange = CarrierValidityError(
        code="FMCSA_contact_change",
        description=prompts.fmcsa_contact_change,
    )

    SellAltLoad = CarrierValidityError(
        code="sell_alt_load",
        description=prompts.sell_alt_load,
    )

    def CheckQualificationFailure(self, item: str) -> CarrierValidityError:
        return CarrierValidityError(
            code="mcleod_carrier_check_qualification_failed",
            description=McleodCheckQualification.description(item=item),
        )

    def HighwayInsuranceUpdate(self, item: str) -> CarrierValidityError:
        print(HighwayInsuranceUpdate.description(item=item))
        return CarrierValidityError(
            code="highway_insurance_update",
            description=HighwayInsuranceUpdate.description(item=item),
        )
