from pydantic import Field
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env", env_file_encoding="utf-8"
    )

    Project: str = Field(alias="APPLICATION__PROJECT")
    Group: str = Field(alias="APPLICATION__GROUP")
    Environment: str = Field(alias="APPLICATION__ENVIRONMENT")
    Revision: str = Field(alias="REVISION")

    LogLevel: str = Field("INFO", alias="LOG_LEVEL")

    OtlpExporterEndpoint: str = Field(
        "https://otel-http.usxpress.io", alias="OTEL_EXPORTER_OTLP_ENDPOINT"
    )

    Host: str = Field("*", alias="HOST")
    Port: int = Field(8080, alias="PORT")

    Clara_HighwayUrl: str = Field(alias="HIGHWAY_URL")
    Clara_HighwayApiKey: str = Field(alias="HIGHWAY_API_KEY")

    Clara_McleodUrl: str = Field(alias="MCLEOD_BASE_URL")
    Clara_McleodAuth: str = Field(alias="MCLEOD_AUTH")
    Clara_McleodCompany: str = Field(alias="MCLEOD_COMPANY")

    Nora_GoogleMapsKml: str = Field(alias="NORA_GOOGLE_MAPS_KML")

    Email_SendgridKey: str = Field(alias="EMAIL_SendgridKey")
    Email_SignatureLogoUrl: str = Field(alias="Email_SignatureLogoUrl")

    KAFKA__api_key: str = Field(alias="KAFKA__api_key")
    KAFKA__api_secret: str = Field(alias="KAFKA__api_secret")
    KAFKA__bootstrap_server: str = Field(alias="KAFKA__bootstrap_server")
    KAFKA__client_id: str = Field(alias="KAFKA__client_id")
    KAFKA__consumer_group: str = Field(alias="KAFKA__consumer_group")
    KAFKA__topic_rapid_alert: str = Field(alias="KAFKA__rapid_alert")

    Elsa_pricing_api: str = Field(alias="ELSA_PRICING_API")
    OnPrem_proxy: str = Field(alias="ONPREM_PROXY")

    Larry_xpm_api: str = Field(alias="LARRY_XPM_API")
