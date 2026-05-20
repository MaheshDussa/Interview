# 07 — Flask Quick API

> **One-liner**: **Flask = minimal, synchronous, classic Python web framework** — small, flexible, huge ecosystem. Pick it for tiny services, legacy apps, or when you don't need async.

---

## 1. Install

```powershell
uv add flask           # or
pip install flask
```

Optional companions:
- `flask-cors` — CORS
- `flask-sqlalchemy` — ORM
- `flask-migrate` — Alembic wrapper
- `flask-jwt-extended` — JWT auth
- `marshmallow` — validation/serialization
- `gunicorn` — production WSGI server

---

## 2. Hello World

```python
# app.py
from flask import Flask, jsonify

app = Flask(__name__)

@app.get("/")
def root():
    return jsonify(message="Hello, World")
```

Run:
```powershell
flask --app app run --debug --port 8000
# or
python -m flask --app app run --debug
```

> `--debug` enables reloader + interactive debugger.

---

## 3. Routes & Methods

```python
from flask import request

@app.route("/items", methods=["GET", "POST"])
def items():
    if request.method == "POST":
        data = request.get_json()
        return jsonify(received=data), 201
    return jsonify(items=["a","b","c"])

# Shorthand decorators
@app.get("/health")
def health(): return jsonify(status="ok")

@app.post("/echo")
def echo(): return jsonify(got=request.get_json()), 201
```

---

## 4. Path & Query Parameters

```python
@app.get("/users/<int:user_id>")
def get_user(user_id: int):
    q = request.args.get("expand", "")
    return jsonify(id=user_id, expand=q)
```

Converters: `<int:x>`, `<float:x>`, `<uuid:x>`, `<path:x>` (allows slashes), `<string:x>` (default).

---

## 5. Request Body & Form Data

```python
@app.post("/users")
def create_user():
    data = request.get_json(silent=True) or {}
    name = data.get("name")
    if not name:
        return jsonify(error="name required"), 400
    return jsonify(id=1, name=name), 201

@app.post("/upload")
def upload():
    f = request.files["file"]            # multipart
    f.save(f"uploads/{f.filename}")
    return "", 204
```

---

## 6. Validation with Pydantic (recommended)

Flask doesn't ship validation; bolt on Pydantic or Marshmallow.

```python
from pydantic import BaseModel, ValidationError, EmailStr

class UserIn(BaseModel):
    name: str
    email: EmailStr
    age: int

@app.post("/users")
def create_user():
    try:
        payload = UserIn.model_validate(request.get_json())
    except ValidationError as e:
        return jsonify(errors=e.errors()), 422
    # use payload.name, payload.email, ...
    return jsonify(payload.model_dump()), 201
```

Or use **Marshmallow** schemas if you prefer.

---

## 7. Error Handling

```python
from werkzeug.exceptions import NotFound

@app.errorhandler(404)
def not_found(e):
    return jsonify(error="not found"), 404

@app.errorhandler(Exception)
def server_error(e):
    app.logger.exception("Unhandled")
    return jsonify(error="internal"), 500

# Raise explicitly
from flask import abort
@app.get("/items/<int:i>")
def get(i):
    if i < 0: abort(404)
    return jsonify(id=i)
```

---

## 8. Blueprints (split big apps)

```python
# api/users.py
from flask import Blueprint, jsonify
bp = Blueprint("users", __name__, url_prefix="/users")

@bp.get("/<int:uid>")
def get(uid): return jsonify(id=uid)

# app.py
from api.users import bp as users_bp
app.register_blueprint(users_bp)
```

---

## 9. Application Factory Pattern

```python
# myapi/__init__.py
from flask import Flask

def create_app(config="myapi.config.Dev") -> Flask:
    app = Flask(__name__)
    app.config.from_object(config)

    from .api.users import bp as users_bp
    app.register_blueprint(users_bp)

    return app
```

```powershell
flask --app "myapi:create_app" run
```

> Factory pattern makes tests and multi-config (dev/test/prod) trivial.

---

## 10. Config

```python
# myapi/config.py
class Base:
    DB_URL = "sqlite:///dev.db"
    JSON_SORT_KEYS = False

class Dev(Base):
    DEBUG = True

class Prod(Base):
    DEBUG = False
    DB_URL = os.environ["DB_URL"]
```

Or load from env file with `python-dotenv` — Flask auto-loads `.env` and `.flaskenv` in debug mode.

---

## 11. Database — Flask-SQLAlchemy

```powershell
pip install flask-sqlalchemy flask-migrate
```

```python
from flask_sqlalchemy import SQLAlchemy
from flask_migrate import Migrate

db = SQLAlchemy()
migrate = Migrate()

class User(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(80))

def create_app():
    app = Flask(__name__)
    app.config["SQLALCHEMY_DATABASE_URI"] = "sqlite:///dev.db"
    db.init_app(app)
    migrate.init_app(app, db)
    return app
```

Migrations:
```powershell
flask db init
flask db migrate -m "init"
flask db upgrade
```

---

## 12. CORS

```python
from flask_cors import CORS
CORS(app, resources={r"/api/*": {"origins": ["https://app.contoso.com"]}})
```

---

## 13. JWT Auth (flask-jwt-extended)

```powershell
pip install flask-jwt-extended
```

```python
from flask_jwt_extended import JWTManager, create_access_token, jwt_required, get_jwt_identity

app.config["JWT_SECRET_KEY"] = "super-secret"
jwt = JWTManager(app)

@app.post("/token")
def login():
    body = request.get_json()
    # validate username/password ...
    token = create_access_token(identity=body["username"])
    return jsonify(access_token=token)

@app.get("/me")
@jwt_required()
def me():
    return jsonify(user=get_jwt_identity())
```

---

## 14. Logging

```python
import logging
logging.basicConfig(level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s: %(message)s")
app.logger.setLevel(logging.INFO)

app.logger.info("Started")
```

> `app.logger` is preconfigured; just set the level.

---

## 15. Testing with `app.test_client()`

```python
def test_root():
    app = create_app("myapi.config.Base")
    client = app.test_client()
    r = client.get("/")
    assert r.status_code == 200
```

Run with pytest: `pytest -q`.

---

## 16. Async in Flask?

Flask 2+ supports `async def` views but the framework itself is still **WSGI / sync**. For real async throughput use **Quart** (a Flask-API-compatible async framework) or **FastAPI**.

```python
@app.get("/slow")
async def slow():
    await asyncio.sleep(1)
    return "ok"
```

---

## 17. Production Server

```powershell
# Gunicorn (Linux)
gunicorn -w 4 -b 0.0.0.0:8000 "myapi:create_app()"

# Waitress (cross-platform incl. Windows)
pip install waitress
waitress-serve --listen=0.0.0.0:8000 --call myapi:create_app
```

Behind Nginx / Application Gateway / Front Door.

---

## 18. Project Layout

```
myapi/
├── src/myapi/
│   ├── __init__.py            # create_app()
│   ├── config.py
│   ├── extensions.py          # db, migrate, jwt = ...
│   ├── api/
│   │   ├── __init__.py
│   │   ├── users.py           # Blueprint
│   │   └── orders.py
│   └── models/
│       └── user.py
├── tests/
├── migrations/                # alembic via flask-migrate
├── pyproject.toml
└── README.md
```

---

## 19. Flask vs FastAPI

| | Flask | FastAPI |
|---|---|---|
| Style | Sync (WSGI) | Async (ASGI) |
| Validation | Bring your own | Pydantic built-in |
| Docs | Manual / Flask-Smorest | Auto OpenAPI/Swagger |
| Speed | Good | Faster (async + Starlette) |
| Ecosystem | Huge, mature | Growing fast |
| Pick when | Tiny app, sync libs, legacy | New service, async I/O, type safety |

---

## 20. Common Pitfalls

| Pitfall | Fix |
|---|---|
| `app` discovered wrong | Use `--app myapi` or factory `--app "myapi:create_app"` |
| Hot-reload doesn't catch import errors | Watch the terminal — Flask prints traceback |
| Sessions don't work | Set `app.secret_key` |
| JSON returns escape unicode | `app.json.ensure_ascii = False` (3.x) |
| Wrong status code | Return `(body, status, headers)` tuple |
| `request.get_json()` is None | Caller forgot `Content-Type: application/json` |
| `flask run` in prod | Don't — use gunicorn/waitress |

---

## 21. Interview / Real-world Q&A

**Q1. Flask vs Django?**
Flask is a micro-framework — you wire up parts. Django is batteries-included (ORM, admin, auth) — opinionated, larger.

**Q2. WSGI vs ASGI?**
WSGI is the sync Python web standard (Flask, Django pre-3.x). ASGI is async (FastAPI, Starlette, Quart, Django 3+ partial).

**Q3. Blueprint purpose?**
Modularize a Flask app — register a group of routes with a URL prefix.

**Q4. Application factory benefit?**
Configurable app creation; easy to spin up multiple instances (tests, configs, plugins).

**Q5. How to add validation?**
Use Pydantic or Marshmallow; Flask itself doesn't validate JSON.

**Q6. How to deploy Flask?**
Behind Gunicorn (Linux) or Waitress (cross-platform); reverse-proxy with Nginx/Front Door/App Gateway.

**Q7. Async Flask?**
Supported syntactically (`async def`) but framework is sync. Use Quart or FastAPI for true async.

**Q8. Auth options?**
Flask-Login (sessions), Flask-JWT-Extended (JWT), Authlib (OAuth/OIDC), or Microsoft Identity Web for Entra.

**Q9. Manage DB schema?**
Flask-Migrate (wraps Alembic) — `flask db init/migrate/upgrade`.

**Q10. Where to put `app.config`?**
A dedicated `config.py` with classes per environment; load via `app.config.from_object(...)` + env overrides.

---

## 22. Cheat Sheet

```powershell
# Setup
uv init flaskapi && cd flaskapi
uv add flask flask-cors flask-sqlalchemy flask-migrate flask-jwt-extended

# Run dev
flask --app "myapi:create_app" run --debug --port 8000

# Run prod
waitress-serve --listen=0.0.0.0:8000 --call myapi:create_app

# Tests
pytest -q
```

---

## 23. Mental Model

> **Flask = `app = Flask(__name__)` + `@app.route` + `request` + `jsonify`. Tiny core, you assemble the rest. Use Blueprints + factory pattern as it grows. Pick FastAPI/Quart for async-heavy services.**
