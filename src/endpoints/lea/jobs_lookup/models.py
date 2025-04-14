from pydantic import BaseModel


class Job(BaseModel):
    name: str
    description: str | None = None
    milesFromLocation: float = 0


class JobLookupResponse(BaseModel):
    jobs: list[Job] = []


class JobLookupRequest(BaseModel):
    city: str
    state: str
    milesFromJob: float = 0


class JobCoords(BaseModel):
    lat: float
    lon: float


class JobLocation(BaseModel):
    name: str
    description: str | None = None
    coords: list[JobCoords]
