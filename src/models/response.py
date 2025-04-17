from typing import Generic, TypeVar

from pydantic import BaseModel

DataType = TypeVar("DataType")


class LLMResponse(BaseModel, Generic[DataType]):
    data: DataType
    schema: DataType
