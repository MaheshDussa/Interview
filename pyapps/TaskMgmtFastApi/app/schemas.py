"""Pydantic request/response models."""
from datetime import date, datetime
from typing import Optional

from pydantic import BaseModel, ConfigDict, Field


class LoginRequest(BaseModel):
    # Plain string (no deliverability/TLD validation) so values like
    # "admin@system.local" are accepted.
    email: str = Field(..., min_length=3, pattern=r"^[^@\s]+@[^@\s]+\.[^@\s]+$")


class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"
    expires_in: int


class CreateTaskRequest(BaseModel):
    title: str = Field(..., max_length=150, min_length=1)
    due_date: Optional[datetime] = Field(default=None, alias="dueDate")

    model_config = ConfigDict(populate_by_name=True)


class UpdateTaskRequest(BaseModel):
    title: str = Field(..., max_length=150, min_length=1)
    is_completed: bool = Field(default=False, alias="isCompleted")
    due_date: Optional[datetime] = Field(default=None, alias="dueDate")

    model_config = ConfigDict(populate_by_name=True)


class TaskResponse(BaseModel):
    taskId: int
    userId: int
    title: str
    isCompleted: Optional[bool] = None
    dueDate: Optional[datetime] = None
    createdAt: Optional[datetime] = None

    model_config = ConfigDict(from_attributes=True)


class WeatherForecast(BaseModel):
    date: date
    temperatureC: int
    temperatureF: int
    summary: Optional[str] = None
