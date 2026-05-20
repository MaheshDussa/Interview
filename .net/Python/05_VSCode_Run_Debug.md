# 05 — Running & Debugging Python in VS Code

> **One-liner**: Pick the right **interpreter**, hit **F5** to debug, set **breakpoints** by clicking the gutter — VS Code handles the rest.

---

## 1. One-time Setup

1. Install Python (see `01_Setup_And_Install.md`).
2. Install VS Code.
3. Install extensions:
   - **Python** (Microsoft)
   - **Pylance** (auto-installed with Python)
   - **Ruff** (lint/format)
   - **Black Formatter** (optional)
   - **Mypy Type Checker** or built-in Pylance
   - **Jupyter** (if you'll use notebooks)
4. Open your project folder (`File → Open Folder…`).

---

## 2. Select Interpreter

The most important step.

- `Ctrl+Shift+P` → **Python: Select Interpreter** → pick `.venv\Scripts\python.exe`.
- The bottom-right status bar shows the active interpreter.
- New integrated terminals auto-activate the venv.

> If the venv isn't listed, click "Enter interpreter path" and browse.

---

## 3. Running Python — three ways

### Way 1 — Run button (▶️)
- Top-right of editor → **Run Python File**.
- Uses currently selected interpreter.

### Way 2 — Terminal
```powershell
python script.py
python -m mypackage          # run a package's __main__.py
uvicorn myapi.main:app --reload
```

### Way 3 — F5 with launch config
- Press **F5** → first time, VS Code asks for a config.
- Pick **Python File** to debug the current file.

---

## 4. `launch.json` — debug configurations

Stored in `.vscode/launch.json`. Auto-generated; edit to add your own.

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Python: Current File",
      "type": "debugpy",
      "request": "launch",
      "program": "${file}",
      "console": "integratedTerminal",
      "justMyCode": true
    },
    {
      "name": "FastAPI (uvicorn)",
      "type": "debugpy",
      "request": "launch",
      "module": "uvicorn",
      "args": ["myapi.main:app", "--reload", "--port", "8000"],
      "jinja": true,
      "console": "integratedTerminal"
    },
    {
      "name": "Flask",
      "type": "debugpy",
      "request": "launch",
      "module": "flask",
      "env": { "FLASK_APP": "app.py", "FLASK_DEBUG": "1" },
      "args": ["run", "--no-debugger", "--no-reload"]
    },
    {
      "name": "Pytest current test",
      "type": "debugpy",
      "request": "launch",
      "module": "pytest",
      "args": ["${file}", "-q"],
      "console": "integratedTerminal"
    },
    {
      "name": "Attach to running process",
      "type": "debugpy",
      "request": "attach",
      "connect": { "host": "localhost", "port": 5678 },
      "pathMappings": [{ "localRoot": "${workspaceFolder}", "remoteRoot": "." }]
    }
  ]
}
```

> Set **`justMyCode: false`** to step into library code.

---

## 5. Breakpoints

| Type | How |
|---|---|
| **Line breakpoint** | Click left gutter — red dot appears |
| **Conditional** | Right-click breakpoint → Add Condition (`x > 5`) |
| **Hit-count** | Right-click → "Hit count" (break on Nth hit) |
| **Logpoint** | Right-click → Add Logpoint — prints w/o stopping (`x = {x}`) |
| **Function breakpoint** | Debug sidebar → + → function name |
| **Exception breakpoint** | Debug sidebar → toggle "Raised Exceptions" / "Uncaught" |

In code (programmatic):
```python
import pdb; pdb.set_trace()       # classic
breakpoint()                      # 3.7+ — picks debugger from PYTHONBREAKPOINT env
```

---

## 6. Debugger Controls

| Key | Action |
|---|---|
| **F5** | Start / continue |
| **F10** | Step Over |
| **F11** | Step Into |
| **Shift+F11** | Step Out |
| **Shift+F5** | Stop |
| **Ctrl+Shift+F5** | Restart |
| **F9** | Toggle breakpoint |

While paused:
- **Variables** pane — current locals/globals.
- **Watch** — pin expressions to monitor.
- **Call Stack** — frames; click to inspect.
- **Debug Console** — run any Python in the paused frame.

---

## 7. Debug Console — the secret weapon

When paused, type any expression and `Enter`:
```
> len(items)
42
> items[0]
{'id': 1, 'name': 'ana'}
> import json; json.dumps(items, indent=2)
```

This lives **inside the paused stack frame**, with all locals.

---

## 8. Debug Web APIs (FastAPI / Flask)

Add the **uvicorn / Flask** launch config (above). When you press F5:
1. The server starts under the debugger.
2. Hit the API from a browser / Postman / `curl`.
3. The breakpoint trips inside your route handler.

> Disable `--reload` while stepping if reload restarts confuse the debugger. (Auto-reload restarts the process, breaking attach.)

---

## 9. Debug Tests (pytest)

VS Code has a built-in **Testing** panel.

1. `Ctrl+Shift+P` → **Python: Configure Tests** → pytest → root folder.
2. The flask icon (Testing) shows tests.
3. Click ▶️ to run, 🐞 to **debug** a test.

`pyproject.toml`:
```toml
[tool.pytest.ini_options]
testpaths = ["tests"]
pythonpath = ["src"]
addopts = "-q"
```

---

## 10. Remote Debugging (attach)

Useful for debugging Docker containers or Azure App Service.

In your app:
```python
# pip install debugpy
import debugpy
debugpy.listen(("0.0.0.0", 5678))
print("Waiting for debugger attach")
debugpy.wait_for_client()
```

Then in VS Code use the **"Attach"** config (port 5678) — set `pathMappings` so breakpoints line up.

For Docker, expose port 5678:
```
docker run -p 8000:8000 -p 5678:5678 myapi
```

---

## 11. Useful `settings.json` (workspace)

```json
{
  "python.defaultInterpreterPath": "${workspaceFolder}/.venv/Scripts/python.exe",
  "python.terminal.activateEnvironment": true,
  "python.analysis.typeCheckingMode": "basic",
  "python.testing.pytestEnabled": true,
  "python.testing.unittestEnabled": false,
  "editor.formatOnSave": true,
  "[python]": {
    "editor.defaultFormatter": "charliermarsh.ruff",
    "editor.codeActionsOnSave": { "source.organizeImports": "explicit" }
  },
  "files.exclude": { "**/__pycache__": true, "**/.pytest_cache": true }
}
```

---

## 12. Environment Variables for Debug

```json
{
  "name": "Run app",
  "type": "debugpy",
  "request": "launch",
  "module": "uvicorn",
  "args": ["myapi.main:app"],
  "env": {
    "DB_URL": "sqlite:///./dev.db",
    "LOG_LEVEL": "DEBUG"
  },
  "envFile": "${workspaceFolder}/.env"
}
```

Use `envFile` to load from `.env` automatically.

---

## 13. Hot Reload Patterns

| Tool | Command |
|---|---|
| Uvicorn | `uvicorn app:app --reload` |
| Flask | `flask run --debug` |
| watchfiles | `watchfiles "python script.py" src/` |
| pytest-watch | `ptw` |

> Reload is great for dev, but **disable** it when stepping through with the debugger.

---

## 14. Common Debug Pitfalls

| Pitfall | Fix |
|---|---|
| Wrong interpreter (no breakpoints hit) | Select venv interpreter; check status bar |
| `ModuleNotFoundError` | Package not installed in this venv; or src-layout needs `pythonpath` |
| Debugger doesn't pause | Check it actually ran the path; add `print` to confirm |
| Breakpoint "unverified" | File path mismatch — usually `pathMappings` in attach config |
| Steps skip into libs | Set `justMyCode: false` |
| Reload kills debugger | Stop reload during debug session |
| `breakpoint()` doesn't work | Set `PYTHONBREAKPOINT=debugpy.breakpoint` or use line breakpoints |
| Tests not discovered | `Python: Configure Tests` again; check `pyproject.toml` `testpaths` |

---

## 15. Profiling (when it's slow)

```powershell
python -m cProfile -o out.prof script.py
python -m pstats out.prof          # interactive

# nicer GUI
pip install snakeviz
snakeviz out.prof
```

Line-level:
```powershell
pip install line_profiler
# decorate function with @profile, then:
kernprof -lv script.py
```

Memory:
```powershell
pip install memory_profiler
python -m memory_profiler script.py
```

---

## 16. Logging in Dev

Keep `print` for quick scripts, but switch to **logging** as soon as your code grows.

```python
import logging
logging.basicConfig(level=logging.DEBUG, format="%(levelname)s %(name)s: %(message)s")
log = logging.getLogger(__name__)

log.debug("Started with %s", config)
```

In VS Code, the **Debug Console** receives stdout/stderr too.

---

## 17. Cheat Sheet

| Goal | Action |
|---|---|
| Run current file | ▶️ Run button or `python file.py` |
| Debug current file | **F5** → Python File |
| Toggle breakpoint | **F9** |
| Step over / into / out | **F10** / **F11** / **Shift+F11** |
| Continue | **F5** |
| Stop | **Shift+F5** |
| Eval expression | Debug Console |
| Run all tests | Testing panel ▶️ or `pytest` |
| Debug a test | Testing panel 🐞 |
| Attach to process | Launch config "attach" + port |

---

## 18. Mental Model

> **Pick the venv → write code → click gutter for breakpoint → F5 → use Variables / Watch / Debug Console while paused. Match the launch config to the entrypoint (script, uvicorn, flask, pytest, attach).**
