# 03 — Python Intermediate

> **One-liner**: Once basics click, learn **OOP, decorators, generators, context managers, async, typing** — these unlock most real-world Python.

---

## 1. Classes & Objects

```python
class Account:
    """A bank account."""

    interest_rate = 0.02              # class attribute (shared)

    def __init__(self, owner: str, balance: float = 0) -> None:
        self.owner = owner            # instance attribute
        self._balance = balance       # _ = "internal use" hint

    def deposit(self, amount: float) -> None:
        if amount <= 0:
            raise ValueError("Amount must be positive")
        self._balance += amount

    @property
    def balance(self) -> float:
        return self._balance

    def __repr__(self) -> str:        # debugging string
        return f"Account({self.owner!r}, {self._balance})"

    def __str__(self) -> str:         # user-facing string
        return f"{self.owner}: ${self._balance:.2f}"

a = Account("Ana", 100)
a.deposit(50)
print(a)               # uses __str__
print(repr(a))         # uses __repr__
```

---

## 2. Inheritance & `super()`

```python
class Savings(Account):
    def __init__(self, owner, balance=0, rate=0.03):
        super().__init__(owner, balance)
        self.rate = rate

    def accrue(self) -> None:
        self._balance *= (1 + self.rate)
```

- Use `isinstance(obj, Savings)` to test.
- Avoid deep inheritance; prefer composition.

---

## 3. Dataclasses (less boilerplate)

```python
from dataclasses import dataclass, field

@dataclass
class Point:
    x: float
    y: float
    tags: list[str] = field(default_factory=list)

p = Point(1.0, 2.0)
print(p)               # Point(x=1.0, y=2.0, tags=[])
```

Variants: `@dataclass(frozen=True)` (immutable), `@dataclass(slots=True)` (memory-friendly).

For runtime validation use **pydantic** (`BaseModel`) instead — heavily used in FastAPI.

---

## 4. Magic / Dunder Methods

| Method | Purpose |
|---|---|
| `__init__` | Constructor |
| `__repr__` / `__str__` | String forms |
| `__eq__`, `__lt__`, … | Comparison |
| `__hash__` | Hashable (dict/set keys) |
| `__len__` | `len(obj)` |
| `__iter__`, `__next__` | Iterable |
| `__getitem__`, `__setitem__` | `obj[key]` |
| `__enter__`, `__exit__` | `with obj:` context manager |
| `__call__` | `obj(...)` callable |

---

## 5. Properties & Setters

```python
class Temperature:
    def __init__(self, celsius: float):
        self.celsius = celsius

    @property
    def fahrenheit(self) -> float:
        return self.celsius * 9/5 + 32

    @fahrenheit.setter
    def fahrenheit(self, value: float) -> None:
        self.celsius = (value - 32) * 5/9
```

---

## 6. Class Methods & Static Methods

```python
class Date:
    def __init__(self, y, m, d): self.y, self.m, self.d = y, m, d

    @classmethod
    def today(cls) -> "Date":
        import datetime as dt
        n = dt.date.today()
        return cls(n.year, n.month, n.day)

    @staticmethod
    def is_leap(year: int) -> bool:
        return year % 4 == 0 and (year % 100 != 0 or year % 400 == 0)
```

---

## 7. Exceptions — Custom

```python
class InsufficientFundsError(Exception):
    def __init__(self, requested, available):
        super().__init__(f"Need {requested}, only {available}")
        self.requested = requested
        self.available = available

try:
    raise InsufficientFundsError(100, 30)
except InsufficientFundsError as e:
    print(e.requested, e.available)
```

> Inherit from `Exception` (not `BaseException`).

---

## 8. Iterators & Generators

### Iterator (manual)
```python
class Counter:
    def __init__(self, n): self.n, self.i = n, 0
    def __iter__(self): return self
    def __next__(self):
        if self.i >= self.n: raise StopIteration
        self.i += 1
        return self.i
```

### Generator (preferred)
```python
def count(n):
    for i in range(1, n+1):
        yield i

for v in count(5): print(v)
```

- Generators are **lazy** — values produced on demand, low memory.
- Use them for streaming large files, paginated APIs, ETL pipelines.

```python
# Generator expression (parentheses instead of brackets)
squares = (x*x for x in range(1_000_000))
```

---

## 9. Comprehensions

```python
# list
evens = [x for x in nums if x % 2 == 0]
# dict
inv = {v: k for k, v in d.items()}
# set
seen = {word.lower() for word in words}
# nested
matrix = [[i*j for j in range(3)] for i in range(3)]
```

> If a comprehension is hard to read, use a loop.

---

## 10. Decorators

A decorator wraps a function with extra behavior.

```python
from functools import wraps
import time

def timed(fn):
    @wraps(fn)
    def wrapper(*args, **kwargs):
        t0 = time.perf_counter()
        try:
            return fn(*args, **kwargs)
        finally:
            print(f"{fn.__name__} took {time.perf_counter()-t0:.3f}s")
    return wrapper

@timed
def heavy():
    sum(i*i for i in range(10_000_000))

heavy()
```

Built-in / common decorators:
- `@property`, `@classmethod`, `@staticmethod`
- `@dataclass`
- `@functools.cache` / `@lru_cache` — memoize
- `@app.get(...)` (FastAPI), `@router.post(...)`

---

## 11. Context Managers

```python
# with-statement, auto cleanup
with open("file.txt") as f:
    data = f.read()

# Make your own
from contextlib import contextmanager

@contextmanager
def db_transaction(conn):
    try:
        yield conn
        conn.commit()
    except:
        conn.rollback()
        raise
```

Use for: files, DB connections, locks, temp dirs, mocking time.

---

## 12. Type Hints (modern)

```python
from typing import Optional, Union, Iterable, Callable, Any, TypedDict, Literal

def get(name: str, default: int | None = None) -> int | None: ...
def add_all(values: Iterable[int]) -> int: ...
on_event: Callable[[str], None] = lambda e: print(e)

class User(TypedDict):
    id: int
    name: str
    role: Literal["admin", "user"]
```

- 3.10+: `int | None` instead of `Optional[int]`.
- Use **`mypy`** or **`pyright`** to check.

### Generics
```python
from typing import Generic, TypeVar
T = TypeVar("T")

class Stack(Generic[T]):
    def __init__(self): self._items: list[T] = []
    def push(self, x: T) -> None: self._items.append(x)
    def pop(self) -> T: return self._items.pop()
```

---

## 13. Async / Await

```python
import asyncio
import httpx

async def fetch(url: str) -> str:
    async with httpx.AsyncClient() as client:
        r = await client.get(url, timeout=10)
        return r.text

async def main():
    urls = ["https://example.com", "https://httpbin.org/uuid"]
    results = await asyncio.gather(*(fetch(u) for u in urls))
    for r in results: print(len(r))

asyncio.run(main())
```

Rules:
- `async def` defines a coroutine; calling it returns a coroutine object.
- `await` only inside `async def`.
- Use `asyncio.gather` for parallel I/O.
- Don't call blocking code inside `async` — wrap with `asyncio.to_thread(fn, ...)`.

---

## 14. Concurrency Models

| Tool | When |
|---|---|
| `asyncio` | High-concurrency I/O (web, DB, APIs) |
| `threading` / `ThreadPoolExecutor` | Blocking I/O, simple parallel |
| `multiprocessing` / `ProcessPoolExecutor` | CPU-bound (bypasses GIL) |
| `subprocess` | Run external programs |
| `concurrent.futures` | Easy threads/processes API |

> The **GIL** means threads don't speed up CPU-bound Python code. Use processes for CPU work.

---

## 15. Logging (use it, don't `print` in production)

```python
import logging

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s - %(message)s",
)
log = logging.getLogger(__name__)

log.info("Started")
log.warning("Low disk")
try:
    1/0
except ZeroDivisionError:
    log.exception("Boom")    # includes traceback
```

- Levels: `DEBUG < INFO < WARNING < ERROR < CRITICAL`.
- Configure once at app startup. Use `__name__` for module loggers.

---

## 16. Useful Standard Libraries

| Module | Use |
|---|---|
| `collections` | `Counter`, `defaultdict`, `deque`, `OrderedDict` |
| `itertools` | `chain`, `groupby`, `combinations`, `product`, `islice` |
| `functools` | `reduce`, `cache`, `lru_cache`, `partial`, `wraps` |
| `pathlib` | Filesystem paths |
| `datetime` | Dates / times / timedelta |
| `re` | Regex |
| `csv` | CSV files |
| `sqlite3` | Built-in DB |
| `enum` | `Enum` class |
| `uuid` | UUID generation |
| `secrets` | Cryptographically-safe random |
| `hashlib` | Hashing (SHA-256, etc.) |
| `tempfile` | Temp files / dirs |

---

## 17. Common Patterns

### Singleton-ish (module-level)
```python
# config.py
settings = load_settings()      # imported anywhere → same instance
```

### Strategy via dict-of-functions
```python
handlers = {"json": handle_json, "csv": handle_csv}
handlers[fmt](data)
```

### Retry with backoff
```python
import time, random
def retry(fn, attempts=3, base=0.5):
    for i in range(attempts):
        try: return fn()
        except Exception:
            if i == attempts - 1: raise
            time.sleep(base * 2**i + random.random()*0.1)
```

---

## 18. Pitfalls & Best Practices

| Pitfall | Fix |
|---|---|
| Circular imports | Move shared code to a 3rd module; import inside functions |
| Modifying default `[]` / `{}` arg | Use `None` sentinel |
| Catching bare `except:` | Catch specific exceptions |
| Long functions | Break into small ones; aim for <30 lines |
| Side effects in module import | Keep top-level pure; use `if __name__ == "__main__":` |
| Mixing sync + async | Don't `time.sleep` in async; use `await asyncio.sleep` |
| Tight loops in pure Python | Use NumPy/Polars or write in C / vectorize |
| Memory blow-up | Switch to generators / streaming |

---

## 19. Quick Interview Q&A

**Q1. List vs Tuple?**
List = mutable, ordered. Tuple = immutable, hashable.

**Q2. Why dataclasses?**
Auto-generated `__init__`, `__repr__`, `__eq__` — less boilerplate for data containers.

**Q3. Difference between `@staticmethod` and `@classmethod`?**
`classmethod` receives `cls` (the class) — useful for factories. `staticmethod` receives nothing — just lives in the class's namespace.

**Q4. What is a generator?**
A function using `yield` that returns values lazily, one at a time, low memory.

**Q5. What is the GIL?**
Global Interpreter Lock — only one thread executes Python bytecode at a time. Threads are fine for I/O; use processes for CPU.

**Q6. Async vs threads?**
Async = single thread, cooperative I/O concurrency. Threads = preemptive, useful for blocking libs. Processes for CPU work.

**Q7. `is` vs `==`?**
`is` = same object identity. `==` = equal values. Use `is None`.

**Q8. Mutable default arg gotcha?**
Defaults evaluated once → shared across calls. Use `None` and assign inside.

**Q9. Context manager?**
Object implementing `__enter__`/`__exit__` (or `@contextmanager` generator) for resource lifecycle inside `with`.

**Q10. What's `__init__.py` for?**
Marks a folder as a package; runs on first import; can re-export symbols.

---

## 20. Mental Model

> **Classes for objects, dataclasses for data, generators for streams, decorators for cross-cutting concerns, async for I/O concurrency, processes for CPU work, type hints + mypy/pyright for safety.**
