# 06 — Building a REST API with FastAPI

> **One-liner**: **FastAPI = modern, async, type-safe Python web framework** — auto docs from type hints, built on Starlette + Pydantic.

---

## 1. Why FastAPI?

| Feature | Benefit |
|---|---|
| **Type hints → validation** | Pydantic auto-validates request/response |
| **Async support** | High concurrency for I/O-bound work |
| **OpenAPI / Swagger UI** | Free interactive docs at `/docs` |
| **Dependency injection** | Built-in, testable |
| **Performance** | Among the fastest Python frameworks (Starlette + uvicorn) |
| **Standards** | OpenAPI, JSON Schema, OAuth2 |

---

## 2. Install

```powershell
# uv (recommended)
uv add fastapi "uvicorn[standard]" pydantic

# pip
pip install fastapi "uvicorn[standard]"
```

> `uvicorn[standard]` adds optional speed deps (websockets, httptools, watchfiles).

For a "batteries-included" install: `pip install "fastapi[standard]"` (adds CLI, jinja, email validation, etc.).

---

## 3. Hello World

```python
# src/myapi/main.py
from fastapi import FastAPI

app = FastAPI(title="My API", version="0.1.0")

@app.get("/")
def root() -> dict[str, str]:
    return {"message": "Hello, World"}
```

Run it:
```powershell
uvicorn myapi.main:app --reload --port 8000
# OR (FastAPI 0.110+ CLI)
fastapi dev src/myapi/main.py
```

Then open:
- `http://localhost:8000/` — endpoint
- `http://localhost:8000/docs` — Swagger UI
- `http://localhost:8000/redoc` — ReDoc
- `http://localhost:8000/openapi.json` — schema

---

## 4. Path & Query Parameters

```python
from fastapi import FastAPI, Query, Path

app = FastAPI()

@app.get("/items/{item_id}")
def get_item(
    item_id: int = Path(..., ge=1, description="Item ID"),
    q: str | None = Query(None, max_length=50),
    limit: int = Query(10, ge=1, le=100),
):
    return {"item_id": item_id, "q": q, "limit": limit}
```

Path params come from URL, query params from `?key=value`.

---

## 5. Request Bodies — Pydantic Models

```python
from pydantic import BaseModel, EmailStr, Field
from datetime import datetime

class UserCreate(BaseModel):
    name: str = Field(min_length=1, max_length=80)
    email: EmailStr
    age: int = Field(ge=0, le=120)

class User(BaseModel):
    id: int
    name: str
    email: EmailStr
    created_at: datetime

@app.post("/users", response_model=User, status_code=201)
def create_user(payload: UserCreate) -> User:
    return User(id=1, name=payload.name, email=payload.email,
                created_at=datetime.utcnow())
```

`EmailStr` requires `pip install "pydantic[email]"`.

---

## 6. Response Models & Status Codes

```python
from fastapi import status

@app.delete("/users/{user_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_user(user_id: int) -> None:
    ...

@app.get("/health", status_code=200)
def health() -> dict:
    return {"status": "ok"}
```

`response_model=` filters the output to match — extra fields are stripped.

---

## 7. Routers (split big apps)

```python
# src/myapi/api/users.py
from fastapi import APIRouter

router = APIRouter(prefix="/users", tags=["users"])

@router.get("/{uid}")
def get(uid: int): ...

@router.post("")
def create(...): ...
```

```python
# main.py
from myapi.api import users
app.include_router(users.router)
```

---

## 8. Dependency Injection

```python
from fastapi import Depends, Header, HTTPException

def get_token(x_api_key: str = Header(...)) -> str:
    if x_api_key != "secret":
        raise HTTPException(401, "Bad key")
    return x_api_key

@app.get("/secure")
def secure(token: str = Depends(get_token)):
    return {"token": token}
```

Use deps for: DB session, current user, settings, feature flags, rate limit checks.

### DB session pattern
```python
from sqlalchemy.orm import Session

def get_db():
    db = SessionLocal()
    try: yield db
    finally: db.close()

@app.get("/users/{uid}")
def get_user(uid: int, db: Session = Depends(get_db)):
    return db.get(User, uid)
```

---

## 9. Async Endpoints

```python
import httpx

@app.get("/weather/{city}")
async def weather(city: str) -> dict:
    async with httpx.AsyncClient(timeout=10) as client:
        r = await client.get(f"https://api.example.com/{city}")
        r.raise_for_status()
        return r.json()
```

- Use `async def` only when you `await` something.
- For blocking libraries (e.g., `psycopg2`, `requests`), keep handlers `def` (FastAPI runs them in a threadpool).

---

## 10. Errors

```python
from fastapi import HTTPException
from fastapi.responses import JSONResponse
from fastapi.requests import Request

@app.get("/items/{id}")
def get(id: int):
    if id < 0:
        raise HTTPException(404, detail="not found")

# Global handler
class BusinessError(Exception):
    def __init__(self, code: str, msg: str): self.code, self.msg = code, msg

@app.exception_handler(BusinessError)
def biz(_: Request, e: BusinessError):
    return JSONResponse({"code": e.code, "msg": e.msg}, status_code=400)
```

---

## 11. Validation & Field Constraints

```python
from pydantic import BaseModel, Field, field_validator

class Order(BaseModel):
    sku: str = Field(pattern=r"^[A-Z]{3}-\d{4}$")
    qty: int = Field(ge=1, le=999)
    note: str | None = Field(default=None, max_length=200)

    @field_validator("note")
    @classmethod
    def strip_note(cls, v):
        return v.strip() if v else v
```

Returns **422 Unprocessable Entity** with details on invalid input — automatically.

---

## 12. Settings & Secrets

```python
# pip install pydantic-settings
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    db_url: str
    log_level: str = "INFO"
    cors_origins: list[str] = []

    class Config:
        env_file = ".env"
        env_file_encoding = "utf-8"

settings = Settings()  # populated from env / .env
```

Inject via dependency to keep handlers testable.

---

## 13. CORS

```python
from fastapi.middleware.cors import CORSMiddleware

app.add_middleware(
    CORSMiddleware,
    allow_origins=["https://app.contoso.com"],
    allow_methods=["*"],
    allow_headers=["*"],
    allow_credentials=True,
)
```

Don't use `allow_origins=["*"]` in production with credentials.

---

## 14. Authentication

### API key
```python
from fastapi.security import APIKeyHeader
api_key_header = APIKeyHeader(name="X-API-Key", auto_error=True)

def require_key(key: str = Depends(api_key_header)) -> None:
    if key != settings.api_key:
        raise HTTPException(401, "Bad key")
```

### OAuth2 / JWT (Bearer)
```python
from fastapi.security import OAuth2PasswordBearer
from jose import jwt, JWTError       # pip install python-jose

oauth2 = OAuth2PasswordBearer(tokenUrl="/token")

def current_user(token: str = Depends(oauth2)) -> dict:
    try:
        payload = jwt.decode(token, KEY, algorithms=["HS256"])
        return payload
    except JWTError:
        raise HTTPException(401, "Invalid token")

@app.get("/me")
def me(user: dict = Depends(current_user)):
    return user
```

For Entra ID, use **`fastapi-azure-auth`** which validates tokens against Microsoft Entra.

---

## 15. Database — SQLAlchemy + Alembic

```powershell
pip install "sqlalchemy>=2" psycopg2-binary alembic
```

```python
# db.py
from sqlalchemy import create_engine, String, Integer
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column, sessionmaker

engine = create_engine(settings.db_url, echo=False)
SessionLocal = sessionmaker(bind=engine, autocommit=False, autoflush=False)

class Base(DeclarativeBase): pass

class UserRow(Base):
    __tablename__ = "users"
    id: Mapped[int] = mapped_column(Integer, primary_key=True)
    name: Mapped[str] = mapped_column(String(80))
```

Migrations:
```powershell
alembic init alembic
alembic revision --autogenerate -m "init"
alembic upgrade head
```

For async: use `sqlalchemy.ext.asyncio` + `asyncpg`.

---

## 16. Background Tasks

```python
from fastapi import BackgroundTasks

def send_email(to: str, body: str): ...

@app.post("/notify")
def notify(to: str, tasks: BackgroundTasks):
    tasks.add_task(send_email, to, "Welcome!")
    return {"queued": True}
```

For heavier jobs use **Celery** or **RQ** with Redis/Service Bus.

---

## 17. WebSockets

```python
from fastapi import WebSocket

@app.websocket("/ws")
async def ws(socket: WebSocket):
    await socket.accept()
    while True:
        msg = await socket.receive_text()
        await socket.send_text(f"echo: {msg}")
```

---

## 18. File Upload / Download

```python
from fastapi import UploadFile, File
from fastapi.responses import FileResponse, StreamingResponse

@app.post("/upload")
async def upload(file: UploadFile = File(...)):
    data = await file.read()
    Path("uploads", file.filename).write_bytes(data)
    return {"size": len(data)}

@app.get("/download/{name}")
def download(name: str):
    return FileResponse(f"uploads/{name}", filename=name)
```

---

## 19. Logging & Middleware

```python
import time, logging
log = logging.getLogger("api")

@app.middleware("http")
async def timing(request, call_next):
    t0 = time.perf_counter()
    response = await call_next(request)
    response.headers["X-Process-Time"] = f"{(time.perf_counter()-t0)*1000:.1f}ms"
    log.info("%s %s -> %s", request.method, request.url.path, response.status_code)
    return response
```

---

## 20. Testing the API

```python
# tests/test_users.py
from fastapi.testclient import TestClient
from myapi.main import app

client = TestClient(app)

def test_create_user():
    r = client.post("/users", json={"name":"A","email":"a@x.com","age":1})
    assert r.status_code == 201
    assert r.json()["name"] == "A"

def test_get_nonexistent():
    assert client.get("/users/9999").status_code == 404
```

Run: `pytest -q`.

---

## 21. Project Layout

```
myapi/
├── src/myapi/
│   ├── __init__.py
│   ├── main.py
│   ├── api/
│   │   ├── __init__.py
│   │   ├── deps.py
│   │   ├── users.py
│   │   └── orders.py
│   ├── core/
│   │   ├── config.py
│   │   └── security.py
│   ├── db/
│   │   ├── base.py
│   │   └── session.py
│   └── models/
│       └── user.py
├── tests/
├── alembic/
├── pyproject.toml
└── README.md
```

---

## 22. Running in Production

```powershell
# 1 worker, dev
uvicorn myapi.main:app --port 8000

# Production behind a load balancer
# Use Gunicorn with uvicorn workers (Linux), or hypercorn / uvicorn directly
gunicorn myapi.main:app -k uvicorn.workers.UvicornWorker -w 4 -b 0.0.0.0:8000
```

Workers = `2 × CPU + 1` for I/O-bound, or 1-per-core. Put it behind **Nginx / Application Gateway / Front Door**.

---

## 23. Health & Readiness

```python
@app.get("/healthz")
def liveness(): return {"status": "ok"}

@app.get("/ready")
def readiness(db: Session = Depends(get_db)):
    db.execute(text("SELECT 1"))
    return {"ready": True}
```

Map these to container probes (K8s / ACA / App Service).

---

## 24. Common Pitfalls

| Pitfall | Fix |
|---|---|
| `async def` with blocking call | Either go async lib or use `def` (threadpool) |
| `--reload` breaks debugger | Disable during debug |
| Importing `app` causes side effects | Keep model loading lazy or under `lifespan` |
| Pydantic v1 syntax with v2 lib | Use v2 (`Field`, `field_validator`, `BaseSettings` from `pydantic-settings`) |
| Sensitive data in OpenAPI | Hide field with `Field(exclude=True)` or split request/response models |
| CORS errors | Add `CORSMiddleware`; check origin scheme + port |
| 422 errors confusing client | Implement custom exception handler for cleaner shape |

---

## 25. Interview / Real-world Q&A

**Q1. Why FastAPI over Flask?**
Async support, type-hint-driven validation, auto OpenAPI, dependency injection.

**Q2. How does FastAPI generate Swagger UI?**
From Pydantic models + decorator metadata → OpenAPI JSON → Swagger UI / ReDoc.

**Q3. async vs sync endpoint?**
`async def` for `await` I/O. Plain `def` runs in a threadpool; safe for blocking libs.

**Q4. Difference between path, query, body, header, cookie parameters?**
Determined by parameter type and default (`Path`, `Query`, `Body`, `Header`, `Cookie`).

**Q5. Dependency injection use case?**
Reuse logic (DB session, auth, settings) across endpoints; easy to override in tests.

**Q6. How do you protect an endpoint?**
Add a security `Depends(...)` that validates a key/token; or use `OAuth2PasswordBearer` with JWT.

**Q7. Validation framework?**
**Pydantic v2** — types, constraints, validators; emits 422 on failures.

**Q8. Background tasks vs Celery?**
`BackgroundTasks` for quick fire-and-forget in same process. Celery for retries, scheduling, separate worker fleet.

**Q9. Hosting in production?**
Gunicorn + uvicorn workers (or hypercorn) behind a reverse proxy; containerized via Docker.

**Q10. How to version an API?**
Path versioning (`/v1/users`), header-based, or separate `APIRouter`s. Path is most common and discoverable.

---

## 26. Cheat Sheet

```powershell
# Create
uv init myapi && cd myapi
uv add "fastapi[standard]" "uvicorn[standard]" pydantic pydantic-settings sqlalchemy

# Run dev
fastapi dev src/myapi/main.py
# or
uvicorn myapi.main:app --reload --port 8000

# Run prod (Linux)
gunicorn myapi.main:app -k uvicorn.workers.UvicornWorker -w 4 -b 0.0.0.0:8000

# Test
pytest -q

# Open docs
start http://localhost:8000/docs
```

---

## 27. Mental Model

> **Define Pydantic models → write `@app.get/post(...)` handlers → FastAPI validates input, runs handler, serializes output, and generates docs. Use Depends() for cross-cutting concerns, async for I/O concurrency, and routers to keep big apps organized.**
