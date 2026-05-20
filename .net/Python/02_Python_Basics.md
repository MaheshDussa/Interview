# 02 — Python Basics

> **One-liner**: Python = readable, dynamically-typed, indentation-based. Everything is an object; whitespace matters.

---

## 1. Hello World

```python
# hello.py
def main() -> None:
    name = "World"
    print(f"Hello, {name}!")

if __name__ == "__main__":
    main()
```

Run: `python hello.py`.

> The `if __name__ == "__main__":` guard means "only execute when this file is run directly, not when imported".

---

## 2. Indentation Is Syntax

```python
if x > 0:
    print("positive")        # 4 spaces, consistent
else:
    print("non-positive")
```

- Use **4 spaces** (PEP 8). Don't mix tabs.
- A block is defined by indentation, not braces.

---

## 3. Variables & Type Hints

```python
age: int = 30
name: str = "Ana"
price: float = 9.99
is_admin: bool = True
nothing: None = None
```

- Type hints are **optional** but strongly recommended.
- Names are `snake_case`; constants are `UPPER_CASE`.

---

## 4. Built-in Types

| Type | Example |
|---|---|
| `int` | `42`, `-7`, `0b1010`, `0x2A` |
| `float` | `3.14`, `1e6` |
| `bool` | `True`, `False` |
| `str` | `"hi"`, `'hi'`, `"""multi"""` |
| `bytes` | `b"raw"` |
| `list` | `[1, 2, 3]` — mutable, ordered |
| `tuple` | `(1, 2, 3)` — immutable, ordered |
| `set` | `{1, 2, 3}` — unique, unordered |
| `dict` | `{"a": 1, "b": 2}` — keyed |
| `None` | absence of value |

---

## 5. Strings

```python
s = "Hello"
len(s)                    # 5
s.upper(), s.lower()
s.startswith("He")
s.replace("l", "L")
",".join(["a","b","c"])   # "a,b,c"
"a,b,c".split(",")        # ["a","b","c"]
s[0], s[-1], s[1:4]       # indexing + slicing
f"{name=} age={age:03d} pi={3.14159:.2f}"   # f-string formatting
```

> **f-strings** are the modern way to format. Avoid `%` and `.format()`.

---

## 6. Numbers & Math

```python
a, b = 7, 2
a + b, a - b, a * b
a / b      # 3.5  (true division)
a // b     # 3    (floor division)
a % b      # 1    (modulo)
a ** b     # 49   (power)

import math
math.sqrt(16), math.pi, math.floor(2.7), math.ceil(2.3)
```

---

## 7. Booleans & Comparison

```python
True and False
True or False
not True
1 < 2 <= 3            # chained
"a" == "a"            # value equality
x is None             # identity (use for None)
```

Falsy values: `False`, `0`, `0.0`, `""`, `None`, `[]`, `{}`, `()`.

---

## 8. Control Flow

```python
if score >= 90:
    grade = "A"
elif score >= 80:
    grade = "B"
else:
    grade = "C"

# Ternary
status = "pass" if score >= 60 else "fail"

# Match (3.10+)
match cmd:
    case "start": start()
    case "stop" | "halt": stop()
    case _: print("unknown")
```

---

## 9. Loops

```python
for i in range(5):                # 0..4
    print(i)

for i, name in enumerate(names):  # index + value
    print(i, name)

for k, v in person.items():
    print(k, v)

while attempts < 3:
    attempts += 1

# break / continue / else
for x in items:
    if x == target: break
else:
    print("not found")            # only runs if no break
```

---

## 10. Lists

```python
nums = [3, 1, 4, 1, 5]
nums.append(9)
nums.extend([2, 6])
nums.insert(0, 99)
nums.remove(1)              # removes first 1
nums.pop()                  # remove & return last
nums.sort()                 # in-place
sorted(nums, reverse=True)  # new list

# Slicing
nums[1:4], nums[::-1], nums[::2]

# Comprehension (preferred)
squares = [x*x for x in range(10) if x % 2 == 0]
```

---

## 11. Tuples

```python
point = (3, 4)
x, y = point                # unpacking
a, *rest = [1,2,3,4]        # a=1, rest=[2,3,4]
```

Immutable, hashable → usable as dict keys / set members.

---

## 12. Dictionaries

```python
person = {"name": "Ana", "age": 30}
person["email"] = "a@x.com"
person.get("phone", "n/a")
"name" in person
del person["age"]

for k, v in person.items():
    ...

# Comprehension
squares = {x: x*x for x in range(5)}

# Merge (3.9+)
merged = a | b
```

---

## 13. Sets

```python
unique = {1, 2, 3}
unique.add(4)
{1,2,3} & {2,3,4}   # intersection
{1,2,3} | {2,3,4}   # union
{1,2,3} - {2,3,4}   # difference
```

---

## 14. Functions

```python
def add(a: int, b: int = 0) -> int:
    """Add two numbers."""
    return a + b

# Keyword args
add(a=2, b=3)

# Variable args
def log(*args, **kwargs):
    print(args, kwargs)

log("a", "b", level="INFO")
# args = ("a","b"), kwargs = {"level":"INFO"}

# Lambda (small anonymous fn)
square = lambda x: x*x
```

> Default arguments are evaluated **once at function definition** — never use mutable defaults like `def f(x=[])`.

---

## 15. Modules & Imports

```python
# math_utils.py
def add(a, b): return a + b

# main.py
import math_utils
from math_utils import add
import math_utils as mu

# Standard library
import os, sys, json
from datetime import datetime, timedelta
```

Run as a module: `python -m mypackage.main`.

---

## 16. Exception Handling

```python
try:
    n = int(value)
except ValueError as e:
    print(f"Bad value: {e}")
except (TypeError, KeyError):
    print("Type/Key error")
else:
    print("All good")           # runs if no exception
finally:
    print("Always runs")        # cleanup

# Raise your own
if n < 0:
    raise ValueError("Must be non-negative")
```

Common exceptions: `ValueError`, `TypeError`, `KeyError`, `IndexError`, `FileNotFoundError`, `ZeroDivisionError`, `AttributeError`.

---

## 17. File I/O

```python
from pathlib import Path

p = Path("data/log.txt")
p.parent.mkdir(parents=True, exist_ok=True)

# Write
p.write_text("hello", encoding="utf-8")

# Read
content = p.read_text(encoding="utf-8")

# Line by line (memory-friendly)
with p.open(encoding="utf-8") as f:
    for line in f:
        print(line.rstrip())

# Binary
data = Path("img.png").read_bytes()
```

> Always use `with` blocks — they auto-close the file.

---

## 18. JSON

```python
import json

s = json.dumps({"a": 1, "b": [1,2]}, indent=2)
obj = json.loads(s)

# To/from file
json.dump(obj, open("out.json","w"))
obj2 = json.load(open("out.json"))
```

---

## 19. Common Built-ins

| Function | Use |
|---|---|
| `len(x)` | Size |
| `type(x)` | Type |
| `isinstance(x, int)` | Type check |
| `range(n)` | 0..n-1 iterator |
| `enumerate(it)` | (index, item) |
| `zip(a, b)` | pair iterables |
| `map(fn, it)` / `filter(fn, it)` | functional |
| `any(it)` / `all(it)` | boolean reduce |
| `sum`, `min`, `max`, `sorted` | aggregates |
| `print(...)` | output |
| `input(prompt)` | read line |

---

## 20. PEP 8 Style Essentials

- 4-space indent, 79-99 char lines.
- Functions / variables → `snake_case`.
- Classes → `PascalCase`.
- Constants → `UPPER_CASE`.
- 2 blank lines between top-level defs, 1 between methods.
- Imports grouped: stdlib → third-party → local; alphabetized.
- One statement per line; prefer explicit over implicit.

> Use **ruff** to enforce automatically.

---

## 21. Common Beginner Pitfalls

| Pitfall | Fix |
|---|---|
| Modifying list while iterating | Iterate over copy: `for x in list(items):` |
| Mutable default arg | Use `None` then set inside: `def f(x=None): x = x or []` |
| `==` vs `is` | `==` value equality; `is` identity (only for None / singletons) |
| Integer division surprise | `/` is float, `//` is floor |
| Off-by-one in `range` | `range(n)` is 0..n-1 |
| Indentation mix tabs+spaces | Configure editor: spaces only |
| Forgetting `self` in method | First arg of instance method must be `self` |
| Catching bare `except:` | Catch specific exceptions |

---

## 22. Quick Drill

```python
# FizzBuzz
for i in range(1, 21):
    out = ""
    if i % 3 == 0: out += "Fizz"
    if i % 5 == 0: out += "Buzz"
    print(out or i)
```

```python
# Word count
from collections import Counter
text = Path("speech.txt").read_text()
counts = Counter(text.lower().split())
print(counts.most_common(10))
```

---

## 23. Mental Model

> **Python = readable English with `:`, indentation, and first-class objects. Master `list`/`dict`/`for`/functions/imports/exceptions first; everything else builds on these.**
