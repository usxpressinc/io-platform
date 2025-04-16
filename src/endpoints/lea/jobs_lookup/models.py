from pydantic import BaseModel


class Job(BaseModel):
    name: str
    description: str | None = None
    milesFromLocation: float | str = 0


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


schema = JobLookupResponse(
    jobs=[
        Job(
            name="This is the summary of the job.",
            description="This is a brief description of the job. Parse through this to find out more details about the job.",
            milesFromLocation="""This is the distance from the job location. If 0, then it is covered in the region.
            If greater than 0, then it is around the region""",
        )
    ]
)
