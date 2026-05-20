# 01 — Python Setup & Installation

> **One-liner**: Install Python once, then **always work inside a virtual environment** so each project has its own packages.

---

## 1. Pick a Python Version

- Use **Python 3.12 or 3.13** (latest stable). Avoid 2.x (dead since 2020).
- Long-term support per version is ~5 years.
- For ML libs check compatibility; sometimes 3.11 is safer for older packages.

---

## 2. Install Python on Windows

### Option A — Official installer (simple)
1. Download from <https://www.python.org/downloads/>.
2. **Check "Add python.exe to PATH"** during install.
3. Install for "all users" if you're admin.

### Option B — Microsoft Store
- `winget install Python.Python.3.12`
- Auto-updates, sandboxed.

### Option C — pyenv-win (manage multiple versions)
```powershell
winget install pyenv-win.pyenv-win
pyenv install 3.12.7
pyenv install 3.11.9
pyenv global 3.12.7        # default
pyenv local 3.11.9         # this folder only (writes .python-version)
```

### Option D — uv (modern, fast — recommended in 2025+)
[uv](https://github.com/astral-sh/uv) is a single binary that handles Python install, virtual envs, and packages much faster than pip.
```powershell
winget install astral-sh.uv
uv python install 3.12
```

---

## 3. Verify Install

```powershell
python --version          # Python 3.12.7
py -0                     # list all installed (Windows py-launcher)
where.exe python          # path to python.exe
python -c "import sys; print(sys.executable)"
```

> On Windows, prefer the **py launcher**: `py -3.12 script.py`.

---

## 4. The "py" Launcher (Windows only)

| Command | What |
|---|---|
| `py` | Newest installed Python |
| `py -3.12` | Specific minor version |
| `py -0` | List installed versions |
| `py -m pip ...` | Run pip with that version |

---

## 5. Pip — the package installer

```powershell
python -m pip install --upgrade pip   # always upgrade first
pip install requests
pip install "fastapi==0.115.*"        # pin
pip uninstall requests
pip list                              # what's installed
pip show fastapi                      # info on one
pip freeze > requirements.txt         # snapshot
```

> **Always use `python -m pip`** (not just `pip`) so it installs into the *currently active* Python.

---

## 6. Virtual Environments — ALWAYS use one

A venv is a folder with its own Python + its own packages — keeps projects isolated.

### Built-in `venv`
```powershell
# Create
python -m venv .venv

# Activate (PowerShell)
.\.venv\Scripts\Activate.ps1

# Activate (cmd.exe)
.\.venv\Scripts\activate.bat

# Deactivate
deactivate
```

After activation:
- `python` and `pip` point inside `.venv`.
- Prompt shows `(.venv)` prefix.

> If activation is blocked: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

### Faster alternatives
| Tool | Why |
|---|---|
| **uv** | Single binary, 10-100× faster than pip |
| **poetry** | Adds lockfiles, build, publish |
| **pipenv** | Older alternative |
| **conda / mamba** | For ML / scientific stacks |

```powershell
# uv flow
uv venv                       # creates .venv with chosen Python
.\.venv\Scripts\Activate.ps1
uv pip install fastapi uvicorn
uv pip compile requirements.in -o requirements.txt
```

---

## 7. `requirements.txt` vs `pyproject.toml`

| File | Used by | What |
|---|---|---|
| **requirements.txt** | pip / uv | Flat list of pinned versions |
| **pyproject.toml** | Modern build tools (PEP 621) | Metadata + deps + tool config |
| **poetry.lock / uv.lock** | Poetry / uv | Exact resolved versions |

### Minimal `pyproject.toml`
```toml
[project]
name = "myapi"
version = "0.1.0"
requires-python = ">=3.12"
dependencies = [
    "fastapi>=0.115",
    "uvicorn[standard]>=0.30",
    "pydantic>=2.8",
]

[project.optional-dependencies]
dev = ["pytest", "ruff", "mypy"]
```

Install from it:
```powershell
pip install -e .            # editable install
pip install -e ".[dev]"     # include dev extras
```

---

## 8. VS Code Setup for Python

1. Install **Python** extension (Microsoft).
2. Install **Pylance** (auto-included).
3. Open the folder → `Ctrl+Shift+P` → **Python: Select Interpreter** → pick `.venv\Scripts\python.exe`.
4. The bottom-right status bar shows the interpreter.
5. New terminal automatically activates the venv.

Recommended extras:
- Ruff (linter/formatter)
- Black formatter
- Mypy / Pyright
- Jupyter (if doing notebooks)
- Even Better TOML

---

## 9. Project Folder Skeleton

```
myapi/
├── .venv/                   # virtual env (gitignored)
├── src/
│   └── myapi/
│       ├── __init__.py
│       └── main.py
├── tests/
│   └── test_main.py
├── pyproject.toml
├── requirements.txt
├── .gitignore
├── .python-version          # pyenv / uv
└── README.md
```

### `.gitignore` essentials
```
.venv/
__pycache__/
*.pyc
.env
.pytest_cache/
.mypy_cache/
.ruff_cache/
dist/
build/
*.egg-info/
```

---

## 10. Environment Variables & `.env`

Store secrets locally without committing them.

```
# .env
API_KEY=abc123
DB_URL=postgresql://user:pass@localhost/app
```

```python
from dotenv import load_dotenv
import os

load_dotenv()
api_key = os.getenv("API_KEY")
```

Install: `pip install python-dotenv`. **Never commit `.env`** — list it in `.gitignore`.

For Pydantic Settings (typed env):
```python
# pip install pydantic-settings
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    api_key: str
    db_url: str
    class Config: env_file = ".env"

settings = Settings()
```

---

## 11. Running Python Code

| Way | Command |
|---|---|
| Run a file | `python script.py` |
| Run a module | `python -m mypackage` |
| Interactive shell | `python` (or `ipython`) |
| Inline | `python -c "print(1+2)"` |
| Jupyter | `jupyter lab` |

---

## 12. Common First-Day Issues

| Symptom | Fix |
|---|---|
| `'python' not recognized` | Re-install with "Add to PATH" ticked, or use `py` launcher |
| Wrong python used | Activate venv; check `where.exe python` |
| `Activate.ps1 cannot be loaded` | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| `pip install` says "Permission denied" | You forgot to activate venv (don't use `--user` system-wide) |
| SSL errors on pip | Corporate proxy — set `HTTPS_PROXY`, `pip config set global.index-url ...` |
| Mixed Python versions | Use **pyenv** or **uv** to pin per-project |
| `ModuleNotFoundError` | Package installed in different venv; verify with `pip list` |

---

## 13. Useful Built-in Modules to Know

| Module | What |
|---|---|
| `os`, `sys` | OS, args, env |
| `pathlib` | Modern file paths |
| `json` | JSON parsing |
| `datetime` | Dates/times |
| `re` | Regex |
| `typing` | Type hints |
| `asyncio` | Async I/O |
| `logging` | Structured logs |
| `argparse` | CLI args |
| `subprocess` | Run other programs |
| `unittest` | Built-in tests |
| `concurrent.futures` | Thread/process pools |

---

## 14. Cheat Sheet

```powershell
# Fresh project (uv flow — recommended)
mkdir myapi; cd myapi
uv init                                # creates pyproject.toml
uv venv
.\.venv\Scripts\Activate.ps1
uv add fastapi "uvicorn[standard]"
uv add --dev pytest ruff mypy
uv run python -m myapi
```

```powershell
# Fresh project (classic flow)
mkdir myapi; cd myapi
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install fastapi "uvicorn[standard]"
pip freeze > requirements.txt
```

---

## 15. Mental Model

> **One Python install (or many via pyenv/uv) → one venv per project → packages live in the venv → VS Code selects the venv → you run/debug from there. Never `pip install` into the system Python.**
