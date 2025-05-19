from pydantic_settings import BaseSettings, SettingsConfigDict


class ClaraPrompts(BaseSettings):
    model_config = SettingsConfigDict(
        env_file="clara.env",
        env_file_encoding="utf-8",
        env_prefix="CLARA_PROMPT_",
        case_sensitive=True,
    )

    multiple_mcleod_carrier: str = "multiple_mcleod_carrier"
    use_compliance_check: str = "use_compliance_check"

    # HighwayInsuranceUpdate
    multi_currency_bipd_requirement: str = "multi_currency_bipd_requirement"
    new_jersey_auto_liability: str = "new_jersey_auto_liability"
    multi_currency_cargo_requirement: str = "multi_currency_cargo_requirement"
    multi_currency_general_liability_requirement: str = (
        "multi_currency_general_liability_requirement"
    )
    has_vin_if_auto_policy_is_scheduled_autos: str = (
        "has_vin_if_auto_policy_is_scheduled_autos"
    )

    # McleodCheckQualification
    is_bond_surety_on_file: str = "is_bond_surety_on_file"
    phmsa_is_verified: str = "phmsa_is_verified"
    interstate_carrier_authority_is_active: str = (
        "interstate_carrier_authority_is_active"
    )
    is_refrigeration_breakdown_included: str = (
        "is_refrigeration_breakdown_included"
    )
    trailer_interchange: str = "trailer_interchange"
    carb_acf: str = "carb_acf"
    carb_tru: str = "carb_tru"

    highway_setup: str = "highway_setup"
    do_not_use: str = "do_not_use"
    highway_connect: str = "highway_connect"
    compliance_check: str = "compliance_check"
    dispatch_connection: str = "dispatch_connection"
    transfer_agent: str = "transfer_agent"
    fmcsa_contact_change: str = "fmcsa_contact_change"
    sell_alt_load: str = "sell_alt_load"

    invalid_dot_mcNumber: str = "invalid_dot_mcNumber"
    use_transfer_to_carrier_sales_rep: str = "use_transfer_to_carrier_sales_rep"

    use_tentative_pass: str = "use_tentative_pass"
    invalid_order: str = "invalid_order"
