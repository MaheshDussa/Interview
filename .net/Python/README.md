# Python — End-to-End Study Track

A 9-file guide from **install** → **language basics** → **packaging** → **VS Code run/debug** → **REST APIs (FastAPI & Flask)** → **testing/quality** → **Docker + Azure deploy**.

Every file follows the same format: ~20 numbered sections, comparison tables, PowerShell-ready snippets, common pitfalls, interview Q&A, and a closing **Mental model**.

---

## Files

| # | File | Covers |
|---|------|--------|
| 01 | [01_Setup_And_Install.md](01_Setup_And_Install.md) | Installing Python (winget / pyenv-win / uv), `py` launcher, pip, venv, VS Code interpreter, first project skeleton, `.gitignore`, common day-1 issues |
| 02 | [02_Python_Basics.md](02_Python_Basics.md) | Indentation, types, strings/f-strings, control flow, `match`, loops, list/tuple/dict/set, functions (`*args`, `**kwargs`), modules, exceptions, `pathlib`, JSON, PEP 8, FizzBuzz |
| 03 | [03_Python_Intermediate.md](03_Python_Intermediate.md) | Classes/dunders/`@property`, inheritance, dataclasses, generators, comprehensions, decorators, context managers, type hints, async/await, threads vs processes vs asyncio, GIL, logging |
| 04 | [04_Packages_And_Envs.md](04_Packages_And_Envs.md) | pip vs uv vs poetry vs conda, `pyproject.toml` (PEP 621), `requirements.txt`, pip-tools, semver, private registries, publishing with `build`+`twine`, `pip-audit`, src-layout |
| 05 | [05_VSCode_Run_Debug.md](05_VSCode_Run_Debug.md) | Python extension, interpreter select, 5 `launch.json` configs (Current File / FastAPI / Flask / Pytest / Attach), breakpoints, Debug Console, remote `debugpy`, hot reload, profiling |
| 06 | [06_REST_API_FastAPI.md](06_REST_API_FastAPI.md) | FastAPI install, Pydantic v2 models, routers, `Depends` DI, SQLAlchemy 2.x + Alembic, JWT/OAuth2, CORS, BackgroundTasks, WebSockets, file upload, TestClient, gunicorn, health probes |
| 07 | [07_Flask_Quick_API.md](07_Flask_Quick_API.md) | Flask routes/methods, request body, Pydantic bolt-on, blueprints, **application factory**, config, Flask-SQLAlchemy + Migrate, CORS, JWT, logging, async caveat, Waitress/Gunicorn |
| 08 | [08_Testing_And_Quality.md](08_Testing_And_Quality.md) | pytest fixtures/parametrize/async, mocking, coverage, **Ruff** lint+format, **mypy/pyright** types, **pre-commit** hooks, GitHub Actions CI, test pyramid, useful plugins |
| 09 | [09_Deploy_Docker_Azure.md](09_Deploy_Docker_Azure.md) | Multi-stage Dockerfile (pip & uv), `.dockerignore`, ACR, **Container Apps / App Service / Functions / AKS**, Managed Identity + Key Vault, App Insights, CI/CD via OIDC, zero-downtime deploys |

---

## Suggested Reading Order

1. **Newcomer to Python** → 01 → 02 → 03 → 04 → 05
2. **Want to build an API fast** → 01 → 05 → 06 (or 07)
3. **Production hardening** → 04 → 08 → 09
4. **Interview prep** → skim all, then re-read Q&A sections of 03, 06, 08, 09

---

## Related — Cloud / AI Notes

| Folder | Topic |
|---|---|
| [../AI200/01_Compute/](../AI200/01_Compute/) | App Service, Functions, VMs |
| [../AI200/02_Containers/](../AI200/02_Containers/) | Docker, ACR, building images without Docker |
| [../AI200/03_Storage_Data/](../AI200/03_Storage_Data/) | Blob Storage, Cosmos DB |
| [../AI200/04_Security_Identity/](../AI200/04_Security_Identity/) | Entra auth/authz, secure Azure solutions |
| [../AI200/05_AZ204_Exam/](../AI200/05_AZ204_Exam/) | 110 AZ-204 practice questions |
| [../AI200/06_AI_Fundamentals/](../AI200/06_AI_Fundamentals/) | AI concepts, GenAI/agents, NLP, speech, CV, info extraction |

---

## Toolchain Recommendation (Modern Python, late 2024)

- **Python**: 3.12 or 3.13 (via `winget install Python.Python.3.12`)
- **Package manager**: **uv** (fastest, replaces pip/pip-tools/poetry for most cases)
- **Linter/formatter**: **Ruff** (replaces flake8 + isort + black)
- **Type checker**: **Pyright** in VS Code; **mypy** in CI
- **Web framework**: **FastAPI** for new APIs (async, typed); **Flask** for tiny/sync
- **Test runner**: **pytest** + `pytest-asyncio` + `pytest-cov`
- **CI**: GitHub Actions with **OIDC to Azure**
- **Runtime in Azure**: **Container Apps** for microservices; **App Service** for single web apps; **Functions** for event-driven

---

## Mental Model

> **Python = batteries-included language + huge ecosystem. Use `uv` to manage envs/deps, `pyproject.toml` for config, `FastAPI` for typed async APIs (or `Flask` for simple sync), `pytest + ruff + mypy` for quality, Docker + Azure Container Apps for deploy. Same mental model as the .NET track — just different tools.**
