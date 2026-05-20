# 04 — Packages, Dependencies & Virtual Environments

> **One-liner**: Every project gets its own **virtual environment** + a **lockable list of dependencies**. Never install into system Python.

---

## 1. The Two Worlds: pip vs uv vs poetry

| Tool | What | Strength |
|---|---|---|
| **pip** | Installs packages | Built-in, universal |
| **venv** | Creates virtual env | Built-in |
| **pip-tools** | Compile lockfile from `requirements.in` | Reproducible |
| **uv** | venv + installer + Python manager | **Fastest, modern (2024+)** |
| **poetry** | Project + deps + build | All-in-one, lockfile |
| **conda / mamba** | Env + packages (incl. C libs) | ML / scientific |

> **Recommendation (2025)**: Use **uv** for new projects. Fall back to `pip + venv` if uv isn't available.

---

## 2. Virtual Environment Recap

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1    # PowerShell
deactivate                       # exit
```

- `.venv\Scripts\python.exe` — the interpreter for this env
- `.venv\Scripts\pip.exe` — pip for this env
- `.venv\Lib\site-packages\` — installed packages live here

Activate symptoms:
- Prompt shows `(.venv)`
- `where.exe python` → `.venv\Scripts\python.exe`

---

## 3. Installing Packages

```powershell
pip install requests
pip install "fastapi>=0.115,<0.116"
pip install "pydantic[email]"            # extra
pip install -U requests                  # upgrade
pip install --pre fastapi                # pre-release
pip install ./local-package              # local folder
pip install "git+https://github.com/user/repo@main"
pip install -r requirements.txt
pip install -e .                         # editable (your own project)
```

Inspect:
```powershell
pip list
pip list --outdated
pip show fastapi
pip check                                # dependency conflicts
```

Uninstall:
```powershell
pip uninstall requests
```

---

## 4. `requirements.txt` (classic)

```
# requirements.txt
fastapi==0.115.0
uvicorn[standard]==0.30.6
pydantic==2.8.2
httpx==0.27.2
```

```powershell
pip freeze > requirements.txt           # capture everything in env
pip install -r requirements.txt
```

> `pip freeze` captures **all** packages including indirect ones. For cleaner lists, use `requirements.in` + `pip-compile`.

---

## 5. `requirements.in` + `pip-compile` (reproducible)

```
# requirements.in       (what YOU asked for)
fastapi
uvicorn[standard]
pydantic
```

```powershell
pip install pip-tools
pip-compile requirements.in -o requirements.txt   # resolves & pins all transitively
pip-sync requirements.txt                         # makes env match exactly
```

---

## 6. `pyproject.toml` (PEP 621 — modern standard)

```toml
[project]
name = "myapi"
version = "0.1.0"
description = "My HTTP API"
requires-python = ">=3.12"
readme = "README.md"
authors = [{ name = "Ana", email = "ana@example.com" }]
dependencies = [
    "fastapi>=0.115",
    "uvicorn[standard]>=0.30",
    "pydantic>=2.8",
    "httpx>=0.27",
]

[project.optional-dependencies]
dev = ["pytest", "pytest-asyncio", "ruff", "mypy", "httpx"]

[project.scripts]
myapi = "myapi.cli:main"      # creates a `myapi` command

[build-system]
requires = ["hatchling"]
backend = "hatchling.build"

[tool.ruff]
line-length = 100

[tool.mypy]
strict = true
```

Install your own project:
```powershell
pip install -e .                # editable (changes reflected immediately)
pip install -e ".[dev]"         # include dev extras
```

---

## 7. uv — the fast modern flow

```powershell
# Install uv once
winget install astral-sh.uv

# New project
uv init myapi
cd myapi

# Manage Python
uv python install 3.12
uv python pin 3.12

# Add / remove deps (auto-writes pyproject.toml + uv.lock)
uv add fastapi "uvicorn[standard]"
uv add --dev pytest ruff mypy
uv remove pytest

# Install/sync to match lockfile
uv sync

# Run a command in the env (without activating)
uv run python -m myapi
uv run pytest

# Drop-in pip replacement (works on requirements.txt)
uv pip install -r requirements.txt
```

**Why uv:** 10-100× faster, single binary, replaces `pip + venv + pip-tools + pyenv`.

---

## 8. Poetry (alternative all-in-one)

```powershell
pipx install poetry
poetry new myapi
cd myapi
poetry add fastapi "uvicorn[standard]"
poetry add --group dev pytest ruff mypy
poetry install
poetry run pytest
poetry shell           # activate env
```

Creates `pyproject.toml` + `poetry.lock`.

---

## 9. Dependency Groups

| Group | Examples |
|---|---|
| Runtime | `fastapi`, `httpx`, `pydantic`, `sqlalchemy` |
| Dev | `pytest`, `ruff`, `black`, `mypy`, `pre-commit` |
| Docs | `mkdocs`, `mkdocs-material` |
| Test | `pytest-cov`, `pytest-asyncio`, `factory_boy` |

Keep them separate so production images stay slim.

---

## 10. Semver & Version Specifiers

| Spec | Means |
|---|---|
| `fastapi==0.115.0` | Exact |
| `fastapi>=0.115` | At least |
| `fastapi~=0.115.0` | Compatible: `>=0.115.0,<0.116.0` |
| `fastapi>=0.115,<0.116` | Range |
| `fastapi[standard]` | Extra |

> **Pin major.minor in libraries**, **pin exact in apps**.

---

## 11. PyPI & Private Registries

- Default index: <https://pypi.org/>
- Test index: <https://test.pypi.org/>
- Private: **Azure Artifacts**, **AWS CodeArtifact**, **GitHub Packages**, self-hosted **devpi**.

Configure:
```ini
# pip.conf  (~/.pip/pip.conf  or  %APPDATA%\pip\pip.ini)
[global]
index-url = https://pypi.org/simple
extra-index-url = https://pkgs.dev.azure.com/<org>/_packaging/<feed>/pypi/simple
```

Token in `keyring` or env var; for Azure Artifacts use `artifacts-keyring`.

---

## 12. Publish Your Own Package

```toml
# pyproject.toml — already has [build-system]
```

```powershell
pip install build twine
python -m build                      # creates dist/*.whl + *.tar.gz
twine upload dist/*                  # to PyPI
twine upload --repository testpypi dist/*
```

For uv: `uv build`, `uv publish`.

---

## 13. Security & Health

| Tool | What |
|---|---|
| `pip-audit` | CVE check on installed packages |
| `safety` | Vulnerability scanner |
| `dependabot` / `renovate` | Auto-PR upgrades on GitHub |
| Repo signing | `pip install --require-hashes` |
| Reproducible builds | Lockfile + `pip install --no-deps` per pinned spec |

```powershell
pip install pip-audit
pip-audit
```

---

## 14. Common Project Layout

```
myapi/
├── src/
│   └── myapi/
│       ├── __init__.py
│       ├── main.py
│       ├── api/
│       │   ├── __init__.py
│       │   └── routes.py
│       ├── core/
│       │   └── config.py
│       └── models/
│           └── user.py
├── tests/
│   ├── __init__.py
│   └── test_api.py
├── pyproject.toml
├── uv.lock              (or poetry.lock / requirements.txt)
├── .python-version
├── .env
├── .gitignore
└── README.md
```

> **src-layout** (above) prevents accidentally importing your package from the project root when running tests.

---

## 15. Common Pitfalls

| Pitfall | Fix |
|---|---|
| `pip install` outside venv | Activate first; check `where.exe python` |
| Different versions on devs' machines | Commit `uv.lock` / `poetry.lock` / pinned `requirements.txt` |
| "Works for me" — missing transitive dep | Use lockfile + clean install in CI |
| Tests can't find package | Use **src-layout** + `pip install -e .` |
| Conflicts between projects | One venv per project (never share) |
| Forgot to update lockfile | Run `uv sync` / `pip-compile` after changing `pyproject.toml` |
| C-extension fails to build | Install MSVC Build Tools / use prebuilt wheels |

---

## 16. Decision Guide

```
Building one app?           → uv (or pip + venv)
Publishing a library?       → poetry or uv (with hatch/flit/hatchling backend)
ML / scientific (CUDA/etc)? → conda / mamba
Multi-project monorepo?     → uv workspaces / Pants / Bazel
```

---

## 17. Cheat Sheet

```powershell
# uv (recommended)
uv init && uv add fastapi "uvicorn[standard]"
uv add --dev pytest ruff mypy
uv sync
uv run uvicorn myapi.main:app --reload

# pip + venv
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -U pip
pip install fastapi "uvicorn[standard]"
pip freeze > requirements.txt
uvicorn myapi.main:app --reload
```

---

## 18. Mental Model

> **`pyproject.toml` declares what you want → tool (uv/poetry/pip-tools) resolves into a lockfile → venv holds the installed copy. Commit the manifest + lockfile, ignore the venv.**
