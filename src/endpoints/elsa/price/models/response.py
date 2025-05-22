from src.models.common import BaseCleanModel


class LookupPriceResponse(BaseCleanModel):
    allInPrice: float | str
    distance: float | str
    error: str | None = None


schema = LookupPriceResponse(
    allInPrice="All in Price for the Request",
    distance="Total Distance for the order",
    error="If failed with error, what error it is",
)
