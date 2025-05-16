from src.prompts import ClaraPrompts

from .models import CarrierValidityError

prompts = ClaraPrompts()


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
                desc = prompts.MultiCurrencyBipdRequirement
            case HighwayInsuranceUpdate.new_jersey_auto_liability:
                desc = prompts.NewJerseyAutoLiability
            case HighwayInsuranceUpdate.multi_currency_cargo_requirement:
                desc = prompts.MultiCurrencyCargoRequirement
            case (
                HighwayInsuranceUpdate.multi_currency_general_liability_requirement
            ):
                desc = prompts.MultiCurrencyGeneralLiabilityRequirement
            case (
                HighwayInsuranceUpdate.has_vin_if_auto_policy_is_scheduled_autos
            ):
                desc = prompts.HasVinIfAutoPolicyIsScheduledAutos
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
                return prompts.IsBondSuretyOnFile
            case McleodCheckQualification.phmsa_is_verified:
                return prompts.PhmsaIsVerified
            case (
                McleodCheckQualification.interstate_carrier_authority_is_active
            ):
                return prompts.InterstateCarrierAuthorityIsActive
            case McleodCheckQualification.is_refrigeration_breakdown_included:
                return prompts.IsRefrigerationBreakdownIncluded
            case McleodCheckQualification.trailer_interchange:
                return prompts.TrailerInterchange
            case McleodCheckQualification.carb_acf:
                return prompts.CarbAcf
            case McleodCheckQualification.carb_tru:
                return prompts.CarbTru
            case _:
                return ""


class Errors:
    HighwaySetup = CarrierValidityError(
        code="highway_setup",
        description=prompts.HighwaySetup,
    )

    DoNotUse = CarrierValidityError(
        code="do_not_use",
        description=prompts.DoNotUse,
    )

    HighwayConnect = CarrierValidityError(
        code="highway_connect",
        description=prompts.HighwayConnect,
    )

    ComplianceCheck = CarrierValidityError(
        code="compliance_check",
        description=prompts.ComplianceCheck,
    )
    DispatchConnection = CarrierValidityError(
        code="dispatch_connection",
        description=prompts.DispatchConnection,
    )
    TransferAgent = CarrierValidityError(
        code="transfer_agent",
        description=prompts.TransferAgent,
    )
    FmcsaContactChange = CarrierValidityError(
        code="FMCSA_contact_change",
        description=prompts.FmcsaContactChange,
    )

    SellAltLoad = CarrierValidityError(
        code="sell_alt_load",
        description=prompts.SellAltLoad,
    )

    def CheckQualificationFailure(self, item: str) -> CarrierValidityError:
        return CarrierValidityError(
            code="mcleod_carrier_check_qualification_failed",
            description=McleodCheckQualification.description(item=item),
        )

    def HighwayInsuranceUpdate(self, item: str) -> CarrierValidityError:
        return CarrierValidityError(
            code="highway_insurance_update",
            description=HighwayInsuranceUpdate.description(item=item),
        )
