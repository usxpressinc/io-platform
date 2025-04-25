from .models import CarrierValidityError


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

    def description(item: str):
        desc = ""
        match item:
            case HighwayInsuranceUpdate.multi_currency_bipd_requirement:
                desc = """Explain that they do not have our minimum BIPD coverage of $1,000,000 USD,
$2,000,000 CAD, or $20,000,000 MXN, then explain if they do have that coverage,
then we are going to work together we need them to get their insurance company send Highway their updated COI.
If they don't have the coverage, we won't be able to work together."""
            case HighwayInsuranceUpdate.new_jersey_auto_liability:
                desc = """Explain that they do not have our minimum BIPD coverage of $1,500,000 USD,
then explain if they do have that coverage, then we are going to work together we need them to get their
insurance company send Highway their updated COI. If they don't have the coverage, we won't be able to work together."""
            case HighwayInsuranceUpdate.multi_currency_cargo_requirement:
                desc = """Explain that they do not have our minimum Cargo coverage of $100,000 USD,
$200,000 CAD, or $2,000,000 MXN, then explain if we are going to work together
we need them to get their insurance company send Highway their updated COI."""
            case (
                HighwayInsuranceUpdate.multi_currency_general_liability_requirement
            ):
                desc = """Explain that they do not have our minimum General Liability insurance coverage of
$1,000,000 USD or $1,500,000 CAD, then explain if we are going to work together we need them
to get their insurance company send Highway their updated COI."""
            case (
                HighwayInsuranceUpdate.has_vin_if_auto_policy_is_scheduled_autos
            ):
                desc = """Explain that they do not have schedule of VINs on COI if auto policy is Scheduled Autos and
the make of the VIN must map to a known power unit, then explain if we are going to work together
we need them to get their insurance company send Highway their updated COI."""
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

    def description(item: str):
        match item:
            case McleodCheckQualification.is_bond_surety_on_file:
                return """Explain that we cannot use them because
they do not have a surety bond on file with the FMCSA."""
            case McleodCheckQualification.phmsa_is_verified:
                return """Explain that we cannot use them because this load is hazmat and
they do not have a PHMSA certification on file with Highway"""
            case (
                McleodCheckQualification.interstate_carrier_authority_is_active
            ):
                return """Explain to the carrier we don't see they have the authority to run interstate and
the load they called about crosses state lines. Don't use "move_on" -
if the carrier wants to dispute this, use the "transfer_to_carrier_sales_rep" tool"""
            case McleodCheckQualification.is_refrigeration_breakdown_included:
                return """Explain we cannot use them because this load is refrigerated and
we cannot use them unless they have refrigerations breakdown coverage"""
            case McleodCheckQualification.trailer_interchange:
                return """Explain we cannot use them because this load is refrigerated and
we cannot use them unless they have refrigerations breakdown coverage"""
            case McleodCheckQualification.carb_acf:
                return """Explain we cannot use them on this load because they aren't Advanced Clean Fleets certified."""
            case McleodCheckQualification.carb_tru:
                return """Explain we cannot use them on this load because they aren't Carb certified."""


class Errors:
    HighwaySetup = CarrierValidityError(
        code="highway_setup",
        description="""Explain that we need them to register with Highway, because Highway manages
our carrier compliance and setup packages. Then use the "highway_setup" tool.
Then ask for the best email to send a link to.""",
    )

    DoNotUse = CarrierValidityError(
        code="do_not_use",
        description="""You cannot use this carrier. If the carrier asks why, you tell them that
you do not have access to that information, all you know is that they are not set up to work with you.
Then politely end the call. We CAN NOT sell them the load and CAN NOT use the 'move_on' tool, under any circumstances.""",
    )

    HighwayConnect = CarrierValidityError(
        code="highway_connect",
        description="""Explain that we need them to complete their highway connection with us.
Then use the 'highway_connect' tool. Then ask for the best email to send a link to.""",
    )

    ComplianceCheck = CarrierValidityError(
        code="compliance_check",
        description="""Explain to the carrier that we need our compliance team to review their account.
Tell the carrier you're submitting a request to the compliance team to review it now &
we will call them back if they get approved. Then use the 'compliance_check' call.""",
    )
    DispatchConnection = CarrierValidityError(
        code="dispatch_connection",
        description="""Explain we can't use them right now because they
have unverified highway users,
they need to login to highway and resolve that before we are able to use them.""",
    )
    TransferAgent = CarrierValidityError(
        code="transfer_agent",
        description="""Explain that we don't show them as being ELD integrated with Highway &
can't use them unless this is resolved. If they dispute this or claim to be exempt,
then use the "transfer_to_carrier_sales_rep" tool""",
    )
    FmcsaContactChange = CarrierValidityError(
        code="FMCSA_contact_change",
        description="""Explain that we have a red flag for them because their FMCSA information has changed.
Ask them to explain what FMCSA info was changed, who made the change, and why was the change made.
Then tell them they need our compliance team to review their account.
Tell the carrier you're submitting a request to the compliance team to review it now &
we will call them back if they get approved. Then use the 'compliance_check' call.""",
    )

    SellAltLoad = CarrierValidityError(
        code="sell_alt_load",
        description="""Explain that this carrier isn't eligible to book this load,
then ask the carrier if they would be interested in any other loads?
If yes, then ask the carrier what direction they want to go then use the 'find_loads_by_lane' tool""",
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
