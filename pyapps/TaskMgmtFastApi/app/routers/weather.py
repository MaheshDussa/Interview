"""Sample WeatherForecast endpoint (parity with the original API)."""
import random
from datetime import date, timedelta

from fastapi import APIRouter

from app.schemas import WeatherForecast

router = APIRouter(tags=["WeatherForecast"])

_SUMMARIES = [
    "Freezing", "Bracing", "Chilly", "Cool", "Mild",
    "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
]


@router.get("/WeatherForecast", response_model=list[WeatherForecast], operation_id="GetWeatherForecast")
def get_weather_forecast() -> list[WeatherForecast]:
    today = date.today()
    return [
        WeatherForecast(
            date=today + timedelta(days=i),
            temperatureC=(c := random.randint(-20, 55)),
            temperatureF=32 + int(c / 0.5556),
            summary=random.choice(_SUMMARIES),
        )
        for i in range(1, 6)
    ]
