from typing import Generic, TypeVar

from src.models.common import BaseCleanModel

DataType = TypeVar("DataType")


class LLMResponse(BaseCleanModel, Generic[DataType]):
    data: DataType
    schema: DataType
