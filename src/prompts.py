from pydantic_settings import BaseSettings, SettingsConfigDict


class ClaraPrompts(BaseSettings):
    model_config = SettingsConfigDict(
        env_file="clara.env",
        env_file_encoding="utf-8",
        env_prefix="CLARA_PROMPT_",
        case_sensitive=True,
        extra="allow",
    )

    MultipleMcleodCarrier: str = "multiple_mcleod_carrier"
    UseComplianceCheck: str = "use_compliance_check"

    # HighwayInsuranceUpdate
    MultiCurrencyBipdRequirement: str = "multi_currency_bipd_requirement"
    NewJerseyAutoLiability: str = "new_jersey_auto_liability"
    MultiCurrencyCargoRequirement: str = "multi_currency_cargo_requirement"
    MultiCurrencyGeneralLiabilityRequirement: str = (
        "multi_currency_general_liability_requirement"
    )
    HasVinIfAutoPolicyIsScheduledAutos: str = (
        "has_vin_if_auto_policy_is_scheduled_autos"
    )

    # McleodCheckQualification
    IsBondSuretyOnFile: str = "is_bond_surety_on_file"
    PhmsaIsVerified: str = "phmsa_is_verified"
    InterstateCarrierAuthorityIsActive: str = (
        "interstate_carrier_authority_is_active"
    )
    IsRefrigerationBreakdownIncluded: str = (
        "is_refrigeration_breakdown_included"
    )
    TrailerInterchange: str = "trailer_interchange"
    CarbAcf: str = "carb_acf"
    CarbTru: str = "carb_tru"

    HighwaySetup: str = "highway_setup"
    DoNotUse: str = "do_not_use"
    HighwayConnect: str = "highway_connect"
    ComplianceCheck: str = "compliance_check"
    DispatchConnection: str = "dispatch_connection"
    TransferAgent: str = "transfer_agent"
    FmcsaContactChange: str = "fmcsa_contact_change"
    SellAltLoad: str = "sell_alt_load"

    InvalidDotMcNumber: str = "invalid_dot_mcNumber"
    UseTransferToCarrierSalesRep: str = "use_transfer_to_carrier_sales_rep"

    UseTentativePass: str = "use_tentative_pass"
    InvalidOrder: str = "invalid_order"
