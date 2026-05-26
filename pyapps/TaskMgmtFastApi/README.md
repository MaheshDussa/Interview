# Task Management FastAPI

Python port of the C# **Task Management API**, providing the same endpoints in a cleaner FastAPI layout and talking to the same `LEARNING` SQL Server database.

## Endpoints

| Method | Path | Description |
| ------ | ---- | ----------- |
| POST   | `/api/Auth/login`   | Email-based login → JWT |
| GET    | `/api/Tasks`        | List current user's tasks |
| POST   | `/api/Tasks`        | Create a task |
| GET    | `/api/Tasks/{id}`   | Get a task by id |
| PUT    | `/api/Tasks/{id}`   | Update a task |
| DELETE | `/api/Tasks/{id}`   | Delete a task |
| GET    | `/WeatherForecast`  | Sample forecast |

Interactive docs: <http://localhost:8000/docs>

## Prerequisites

- Python 3.11+
- Microsoft **ODBC Driver 17 for SQL Server** installed
- SQL Server instance reachable at `localhost\SQLEXPRESS01` with the `LEARNING` database and the `Users` / `Tasks` tables already present

Connection string used (Windows Authentication):

```
Data Source=localhost\SQLEXPRESS01;Initial Catalog=LEARNING;Integrated Security=True
```

## Install & Run

```powershell
cd c:\Users\mdussa\Desktop\WOW-Team1\pyapps\TaskMgmtFastApi
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python run.py
```

## Project Layout

```
app/
  main.py          FastAPI app + OpenAPI customisation
  config.py        Settings (connection string, JWT)
  database.py      SQLAlchemy engine + session
  models.py        ORM models (User, UserTask)
  schemas.py       Pydantic request/response models
  security.py      JWT creation + auth dependency
  routers/
    auth.py        /api/Auth/login
    tasks.py       /api/Tasks CRUD
    weather.py     /WeatherForecast
run.py             uvicorn launcher
requirements.txt
```

## Notes

- Login is email-only (mirrors the source API). The user must exist in `Users` and have `IsActive = 1`.
- Tasks are automatically scoped to the authenticated user via the JWT `sub` claim.
- Set `JWT_SECRET` via environment in production rather than relying on the default in `config.py`.
