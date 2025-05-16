from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings, SettingsConfigDict
from pygeoif.types import LineType, Point2D, Point3D

from src.models.common import BaseCleanModel


class Job(BaseCleanModel):
    name: str
    description: str | None = None
    milesFromLocation: float | str = 0


class JobLookupResponse(BaseCleanModel):
    jobs: list[Job] = []


class JobLookupRequest(BaseModel):
    city: str
    state: str
    milesFromJob: float = 0


class JobCoords(BaseModel):
    lat: LineType | float | Point2D | Point3D
    lon: LineType | float | Point2D | Point3D


class JobLocation(BaseModel):
    name: str
    description: str | None = None
    coords: list[JobCoords]


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=".env", env_file_encoding="utf-8"
    )

    JobLookupResponse_desc: str = Field(
        """This is a brief description of the job.
Parse through this to find out more details about the job.
Convert HTML to plain text.
Removing all special characters and smart quotes (like â€™, Äô, etc.).
Normalizing the text so it's clean and readable (no junk characters).
Then use this to provide info to the user.
Replace <br> tags with a short pause or period; ignore any other HTML tags entirely.
Treat •, , or tab characters as a comma-length pause, then continue reading.
Skip any line made only of underscores or dashes.
Convert ALL-CAPS words to normal case before speaking;
render multiple exclamation points as enthusiastic tone, not spoken punctuation.
Speak two-letter state codes as full state names;
spell alphanumeric zone codes character-by-character; shorten long state lists to “including … and others.”
Summarize table or mileage-grid rows into plain sentences
(“If your team runs twenty-four to twenty-six thousand miles, you earn an extra ten cents per mile split”); omit column headers.
Read “$0.55 CPM” as “fifty-five cents per mile,” and read standalone dollar amounts normally (“twelve dollars and fifty cents”).
Do not read raw URLs aloud; instead say “see the link we sent you.”
Skip metadata tags like “Tagging,” “South Florida Y/N,” or date stamps unless the driver asks for them.
Summarize long legal or compliance blocks into one sentence about regulatory compliance.
If unsure how to pronounce an unfamiliar string, spell it slowly, then offer to text or email the details.
When you move on from one job opportunity to the next,
use transition words or phrases like “That's the first one, the second one I have for you is…” or ”Next I have a…"
.""",
        alias="JobLookupResponse_desc",
    )

    JobLookupResponse_name: str = Field(
        """This is the summary of the job.
Don't list things
Parse through this to find out more details about the job.
Convert HTML to plain text.
Removing all special characters and smart quotes (like â€™, Äô, etc.).
Normalizing the text so it's clean and readable (no junk characters).
Then use this to provide info to the user.
Replace <br> tags with a short pause or period; ignore any other HTML tags entirely.
Treat •, , or tab characters as a comma-length pause, then continue reading.
Skip any line made only of underscores or dashes.
Convert ALL-CAPS words to normal case before speaking;
render multiple exclamation points as enthusiastic tone, not spoken punctuation.
Speak two-letter state codes as full state names;
spell alphanumeric zone codes character-by-character; shorten long state lists to “including … and others.”
Summarize table or mileage-grid rows into plain sentences
(“If your team runs twenty-four to twenty-six thousand miles, you earn an extra ten cents per mile split”); omit column headers.
Read “$0.55 CPM” as “fifty-five cents per mile,” and read standalone dollar amounts normally (“twelve dollars and fifty cents”).
Do not read raw URLs aloud; instead say “see the link we sent you.”
Skip metadata tags like “Tagging,” “South Florida Y/N,” or date stamps unless the driver asks for them.
Summarize long legal or compliance blocks into one sentence about regulatory compliance.
If unsure how to pronounce an unfamiliar string, spell it slowly, then offer to text or email the details.
""",
        alias="JobLookupResponse_name",
    )


schema = JobLookupResponse(
    jobs=[
        Job(
            name=Settings.model_validate({}).JobLookupResponse_name,
            description=Settings.model_validate({}).JobLookupResponse_desc,
            milesFromLocation="""This is the distance from the job location. If 0, then it is covered in the region.
            If greater than 0, then it is around the region""",
        )
    ]
)
