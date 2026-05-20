# 08 — Testing & Code Quality

> **One-liner**: **pytest** for tests, **ruff** for lint+format, **mypy/pyright** for types, **coverage** for completeness, **pre-commit** to run all of it on every commit.

---

## 1. The Modern Quality Stack

| Tool | Job |
|---|---|
| **pytest** | Test runner |
| **pytest-asyncio** | Async tests |
| **pytest-cov** | Coverage |
| **httpx / TestClient** | API tests |
| **ruff** | Lint + format (replaces flake8/isort/black) |
| **black** | Format (if not using ruff format) |
| **mypy** or **pyright** | Static type checking |
| **pre-commit** | Run hooks on `git commit` |
| **tox / nox** | Test across Python versions |

Install (dev group):
```powershell
uv add --dev pytest pytest-asyncio pytest-cov httpx ruff mypy pre-commit
```

---

## 2. pytest Basics

```python
# tests/test_math.py
def add(a, b): return a + b

def test_add():
    assert add(2, 3) == 5

def test_add_zero():
    assert add(0, 0) == 0
```

Run:
```powershell
pytest                  # all tests
pytest -q               # quiet
pytest -k "user"        # match name substring
pytest -x               # stop at first fail
pytest --lf             # only last-failed
pytest -vv              # verbose
pytest tests/test_x.py::TestY::test_z
```

Discovery: pytest finds `test_*.py` files and `test_*` functions / `Test*` classes.

---

## 3. Assertions & Failures

```python
def test_dict():
    d = {"a": 1, "b": 2}
    assert d == {"a": 1, "b": 2}         # rich diff on failure
    assert "a" in d
    assert d.get("c", 0) == 0
```

> pytest rewrites `assert` to give detailed failure messages — don't use `unittest.assertEqual` style.

---

## 4. Fixtures (setup / teardown)

```python
import pytest

@pytest.fixture
def db():
    print("setup")
    conn = create_conn()
    yield conn
    conn.close()
    print("teardown")

def test_query(db):
    assert db.execute("select 1") == 1
```

Scopes: `function` (default), `class`, `module`, `session`.

```python
@pytest.fixture(scope="session")
def app():
    return create_app("test")
```

### conftest.py
Place shared fixtures in `tests/conftest.py` — auto-discovered.

```python
# tests/conftest.py
import pytest
from myapi import create_app
from fastapi.testclient import TestClient

@pytest.fixture
def client():
    return TestClient(create_app())
```

---

## 5. Parametrize

```python
@pytest.mark.parametrize("a,b,expected", [
    (1, 2, 3),
    (0, 0, 0),
    (-1, 1, 0),
])
def test_add(a, b, expected):
    assert a + b == expected
```

Test name becomes `test_add[1-2-3]`, etc.

---

## 6. Expecting Exceptions

```python
def test_raises():
    with pytest.raises(ValueError, match="negative"):
        do_thing(-1)
```

---

## 7. Mocking (`unittest.mock`)

```python
from unittest.mock import patch, MagicMock

def test_email(monkeypatch):
    mock = MagicMock()
    monkeypatch.setattr("myapi.email.send", mock)
    notify("a@x.com")
    mock.assert_called_once_with("a@x.com")
```

`patch` decorator:
```python
@patch("myapi.svc.requests.get")
def test_fetch(mock_get):
    mock_get.return_value.json.return_value = {"ok": True}
    assert fetch() == {"ok": True}
```

> Mock **where it's used**, not where it's defined.

---

## 8. Async Tests

```python
import pytest, httpx

@pytest.mark.asyncio
async def test_async():
    async with httpx.AsyncClient() as c:
        r = await c.get("https://example.com")
        assert r.status_code == 200
```

```toml
# pyproject.toml
[tool.pytest.ini_options]
asyncio_mode = "auto"        # marks async tests automatically
```

---

## 9. Testing FastAPI / Flask

### FastAPI
```python
from fastapi.testclient import TestClient
from myapi.main import app

def test_users(client=TestClient(app)):
    r = client.post("/users", json={"name":"A","email":"a@x.com","age":1})
    assert r.status_code == 201
```

### Flask
```python
def test_health(app):
    client = app.test_client()
    assert client.get("/health").status_code == 200
```

Override dependencies (FastAPI):
```python
app.dependency_overrides[get_db] = lambda: fake_db
```

---

## 10. Coverage

```powershell
pytest --cov=src --cov-report=term-missing --cov-report=html
# open htmlcov/index.html
```

```toml
[tool.coverage.run]
source = ["src"]
branch = true
[tool.coverage.report]
fail_under = 85
show_missing = true
exclude_lines = ["pragma: no cover", "raise NotImplementedError"]
```

> Set `fail_under` in CI so PRs can't drop coverage.

---

## 11. Linting & Formatting — Ruff

Ruff replaces flake8, isort, pydocstyle, parts of pylint, and includes a fast formatter.

```toml
[tool.ruff]
line-length = 100
target-version = "py312"

[tool.ruff.lint]
select = ["E","F","W","I","UP","B","SIM","TID","ASYNC","S","PL"]
ignore = ["S101"]                # allow assert in tests

[tool.ruff.lint.per-file-ignores]
"tests/*" = ["S", "PL"]
```

Run:
```powershell
ruff check .                  # lint
ruff check . --fix            # autofix
ruff format .                 # format
```

---

## 12. Black (alternative formatter)

```powershell
black src tests
```

```toml
[tool.black]
line-length = 100
target-version = ["py312"]
```

> If using Ruff's formatter, you don't need Black.

---

## 13. Type Checking — Mypy / Pyright

```toml
[tool.mypy]
python_version = "3.12"
strict = true
files = ["src"]
exclude = ["tests"]
```

```powershell
mypy
```

Pyright (Pylance under the hood) is faster, runs in VS Code automatically. Switch in settings:
```json
"python.analysis.typeCheckingMode": "strict"
```

---

## 14. Security Scanning

```powershell
pip install pip-audit bandit
pip-audit                       # CVEs in deps
bandit -r src                   # security issues in code
```

---

## 15. pre-commit Hooks

Install once:
```powershell
pip install pre-commit
pre-commit install
```

`.pre-commit-config.yaml`:
```yaml
repos:
  - repo: https://github.com/astral-sh/ruff-pre-commit
    rev: v0.6.9
    hooks:
      - id: ruff
        args: [--fix]
      - id: ruff-format
  - repo: https://github.com/pre-commit/mirrors-mypy
    rev: v1.11.2
    hooks:
      - id: mypy
        additional_dependencies: [pydantic]
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.6.0
    hooks:
      - id: end-of-file-fixer
      - id: trailing-whitespace
      - id: check-merge-conflict
      - id: check-yaml
```

Run on demand:
```powershell
pre-commit run --all-files
```

Every `git commit` now runs the hooks.

---

## 16. Test Across Python Versions (tox / nox)

```ini
# tox.ini
[tox]
envlist = py311, py312, py313
[testenv]
deps = pytest
commands = pytest -q
```

```powershell
pip install tox
tox
```

Nox is similar but uses Python config files.

---

## 17. Continuous Integration (GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: ci
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    strategy:
      matrix: { python: ["3.11","3.12","3.13"] }
    steps:
      - uses: actions/checkout@v4
      - uses: astral-sh/setup-uv@v3
      - uses: actions/setup-python@v5
        with: { python-version: "${{ matrix.python }}" }
      - run: uv sync --all-extras --dev
      - run: uv run ruff check .
      - run: uv run mypy
      - run: uv run pytest --cov=src --cov-report=xml
      - uses: codecov/codecov-action@v4
```

---

## 18. Test Pyramid

```
    /\         E2E         (few)
   /  \        Integration (some)
  /____\       Unit        (many)
```

- **Unit**: pure functions, classes, no I/O. Fast.
- **Integration**: real DB / API / file system. Slower.
- **E2E**: full app + dependencies, like Playwright + running server.

Keep slow tests under a marker so devs can skip locally:
```python
@pytest.mark.slow
def test_full_flow(): ...
```
```powershell
pytest -m "not slow"
```

---

## 19. Useful pytest Plugins

| Plugin | Use |
|---|---|
| `pytest-cov` | Coverage |
| `pytest-asyncio` | async tests |
| `pytest-mock` | Pytest-style mocker fixture |
| `pytest-xdist` | Parallel test run |
| `pytest-randomly` | Randomize order to find hidden coupling |
| `pytest-benchmark` | Microbenchmarks |
| `pytest-httpx` | Mock httpx |
| `freezegun` / `time-machine` | Freeze time |
| `factory-boy` / `faker` | Test data |
| `respx` | Mock httpx for async |

---

## 20. Doctest (lightweight inline tests)

```python
def add(a, b):
    """Add two numbers.

    >>> add(2, 3)
    5
    """
    return a + b
```

Run: `pytest --doctest-modules` or `python -m doctest -v module.py`.

---

## 21. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Tests pass locally, fail in CI | Pin deps via lockfile; same Python; clean env |
| Order-dependent tests | `pytest-randomly` to expose; isolate state |
| Patching wrong path | Patch the symbol where used, not defined |
| Real network in tests | Use `responses` / `respx` / `pytest-httpx` |
| Flaky time-based tests | Freeze time with `freezegun` |
| Slow suite | Parallelize with `pytest-xdist -n auto` |
| `assert` stripped | Don't run pytest under `python -O` |
| `ModuleNotFoundError` for src-layout | Set `pythonpath = ["src"]` in `pyproject.toml` |

---

## 22. Interview Q&A

**Q1. Why pytest over unittest?**
Plain `assert`, fixtures, parametrize, plugins, rich output.

**Q2. Fixture vs setUp/tearDown?**
Fixtures are reusable, composable, and scoped (function/class/module/session); cleaner than `setUp`.

**Q3. Mock vs MagicMock vs patch?**
`Mock` = generic; `MagicMock` = adds magic methods; `patch` = context/decorator to replace an object during a test.

**Q4. How to test an async function?**
`pytest-asyncio` + `@pytest.mark.asyncio` (or `asyncio_mode = "auto"`).

**Q5. Difference between unit and integration tests?**
Unit isolates code with mocks; integration uses real dependencies (DB, API).

**Q6. What's coverage measuring?**
Lines (and optionally branches) of source executed by tests. Not a quality guarantee — high coverage with bad assertions is still bad.

**Q7. Why Ruff?**
Single fast tool replacing flake8 + isort + black + parts of pylint; runs in milliseconds.

**Q8. Static typing in Python?**
Optional via type hints; enforced by **mypy** or **pyright**. Catches whole classes of bugs at edit time.

**Q9. What is pre-commit?**
Git hooks framework that runs linters/formatters/tests on staged files before commit.

**Q10. CI strategy for Python?**
Lockfile install → lint → type check → test (matrix Pythons) → upload coverage → build artifact.

---

## 23. Cheat Sheet

```powershell
# Install dev tools
uv add --dev pytest pytest-asyncio pytest-cov httpx ruff mypy pre-commit

# Run
pytest -q
pytest --cov=src --cov-report=term-missing
ruff check . --fix && ruff format .
mypy
pre-commit run --all-files
```

---

## 24. Mental Model

> **Write small unit tests first, fixtures for setup, parametrize for cases. Lint with Ruff, type-check with Mypy/Pyright, measure with coverage, enforce on every commit via pre-commit + CI matrix.**
