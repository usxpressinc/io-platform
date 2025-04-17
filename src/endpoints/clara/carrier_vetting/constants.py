from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Errors(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env", env_file_encoding="utf-8"
    )

    HighwaySetup: str = Field("highway_setup", alias="CARRIER_ERROR_Setup")
    HighwayConnect: str = Field(
        "highway_connect", alias="CARRIER_ERROR_Connect"
    )
    HighwayInsuranceUpdate: str = Field(
        "highway_insurance_update", alias="CARRIER_ERROR_HighwayInsuranceUpdate"
    )

    DoNotUse: str = Field("do_not_use", alias="CARRIER_ERROR_DoNotUse")
    MoveOn: str = Field("move_on", alias="CARRIER_ERROR_MoveOn")
    SellAltLoad: str = Field("sell_alt_load", alias="CARRIER_ERROR_SellAltLoad")
    ComplianceCheck: str = Field(
        "compliance_check", alias="CARRIER_ERROR_ComplianceCheck"
    )
    TransferAgent: str = Field(
        "transfer_agent", alias="CARRIER_ERROR_TransferAgent"
    )
    FmcsaContactChange: str = Field(
        "FMCSA_contact_change", alias="CARRIER_ERROR_FmcsaContactChange"
    )
    DispatchConnection: str = Field(
        "dispatch_connection", alias="CARRIER_ERROR_DispatchConnection"
    )

    McleodNoCarrier: str = Field(
        "no_mcleod_carrier", alias="CARRIER_ERROR_McleodNoCarrier"
    )
    McleodManyCarrier: str = Field(
        "multiple_mcleod_carrier", alias="CARRIER_ERROR_McleodManyCarrier"
    )
