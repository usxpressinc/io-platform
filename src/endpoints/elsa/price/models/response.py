from src.models.common import BaseCleanModel


class Response(BaseCleanModel):
    price: int | str | None
    error: str | None


schema = Response(
    price="All in Price for the Request",
    error="If failed with error, what error it is",
)
