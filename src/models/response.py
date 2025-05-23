from typing import Any, Generic, TypeVar

from src.models.common import BaseCleanModel

DataType = TypeVar("DataType")


class LLMResponse(BaseCleanModel, Generic[DataType]):
    data: DataType | Any
    response_schema: DataType
