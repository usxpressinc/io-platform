from pydantic import BaseModel, model_validator


class BaseCleanModel(BaseModel):
    @model_validator(mode="before")
    @classmethod
    def clean_strings(cls, data):
        return cls._clean_data(data)

    @classmethod
    def _clean_data(cls, value):
        if isinstance(value, str):
            return cls.clean_new_lines(value)
        elif isinstance(value, dict):
            return {k: cls._clean_data(v) for k, v in value.items()}
        elif isinstance(value, list):
            return [cls._clean_data(v) for v in value]
        return value

    @classmethod
    def clean_new_lines(cls, string: str) -> str:
        return " ".join(string.split()).replace("\n", " ").replace("\r", "")
