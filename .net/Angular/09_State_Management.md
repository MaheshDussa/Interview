# 09 — State Management (Signals, Services, NgRx)

> **One-liner**: Match the tool to the scale — **signal in component → signal-based service → SignalStore / NgRx** as state grows. Don't reach for NgRx on day one.

---

## 1. The State Ladder (start small, climb when needed)

| Scope | Pattern | Use when |
|---|---|---|
| **Single component** | Component fields / signals | Local UI state (open/closed, current tab) |
| **Component + children** | `@Component({ providers: [Store] })` + signals | Wizard, modal subtree |
| **Whole feature** | Route-scoped service + signals | Orders list + detail share state |
| **Whole app, simple** | Root-provided service with signals | Auth user, theme |
| **Whole app, complex** | **NgRx SignalStore** | Multiple sources, derived data, optimistic UI |
| **Whole app, enterprise** | **NgRx Store + Effects** | Strict event sourcing, time-travel debug, large teams |

> Senior interview line: "I default to a signal-based service. I introduce NgRx (or SignalStore) only when I see derived data fanning out to many components, async coordination, or undo/redo requirements."

---

## 2. Component-Local State with Signals

```ts
@Component({ template: `
  <button (click)="inc()">{{ count() }} ({{ double() }})</button>` })
export class CounterComponent {
  count  = signal(0);
  double = computed(() => this.count() * 2);

  inc() { this.count.update(c => c + 1); }
}
```

`signal` = writable cell.
`computed` = derived (recomputes only when deps change, cached).
`effect` = side-effect when reads change.

```ts
effect(() => console.log('count is', this.count()));
```

Effects run in injection context and clean up automatically when destroyed.

---

## 3. Service-Based Store (the practical default)

```ts
@Injectable({ providedIn: 'root' })
export class CartStore {
  private http = inject(HttpClient);

  // private writable state
  private _items   = signal<CartItem[]>([]);
  private _loading = signal(false);
  private _error   = signal<string | null>(null);

  // public readonly views
  readonly items   = this._items.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error   = this._error.asReadonly();

  // derived
  readonly total = computed(() => this._items().reduce((s, i) => s + i.price * i.qty, 0));
  readonly count = computed(() => this._items().reduce((s, i) => s + i.qty, 0));

  // actions
  load() {
    this._loading.set(true);
    this.http.get<CartItem[]>('/api/cart').pipe(
      tap(items => this._items.set(items)),
      catchError(err => { this._error.set(err.message); return of([]); }),
      finalize(() => this._loading.set(false))
    ).subscribe();
  }

  add(item: CartItem) {
    this._items.update(arr => [...arr, item]);
  }

  remove(id: string) {
    this._items.update(arr => arr.filter(i => i.id !== id));
  }

  clear() { this._items.set([]); }
}
```

Why this is enough most of the time:
- Strongly typed
- No external library
- Auto-CD (signals integrate with templates)
- Easy to test (no boilerplate)

---

## 4. Sharing State Between Components

Inject the same singleton:
```ts
@Component({ ... })
export class HeaderComponent {
  cart = inject(CartStore);     // count() in template
}
@Component({ ... })
export class CartPageComponent {
  cart = inject(CartStore);     // items() + total()
}
```

Or scope to a feature via route:
```ts
{ path: 'orders', providers: [OrdersStore], children: [...] }
```

---

## 5. NgRx SignalStore (modern, lightweight)

Install:
```powershell
npm i @ngrx/signals
```

```ts
import { signalStore, withState, withMethods, withComputed, patchState } from '@ngrx/signals';

type CartState = { items: CartItem[]; loading: boolean; error: string | null; };

export const CartStore = signalStore(
  { providedIn: 'root' },
  withState<CartState>({ items: [], loading: false, error: null }),
  withComputed(({ items }) => ({
    total: computed(() => items().reduce((s, i) => s + i.price * i.qty, 0)),
    count: computed(() => items().reduce((s, i) => s + i.qty, 0))
  })),
  withMethods((store, http = inject(HttpClient)) => ({
    load() {
      patchState(store, { loading: true });
      http.get<CartItem[]>('/api/cart').subscribe({
        next: items => patchState(store, { items, loading: false }),
        error: err  => patchState(store, { error: err.message, loading: false })
      });
    },
    add(i: CartItem) { patchState(store, s => ({ items: [...s.items, i] })); }
  }))
);

// Inject anywhere
cart = inject(CartStore);
// cart.items(), cart.total(), cart.add(...)
```

Why SignalStore: less boilerplate than classic NgRx, fully signal-native, supports entities (`withEntities`), RxJS bridge (`rxMethod`).

---

## 6. Classic NgRx Store (Redux pattern)

When to use: large team, strict event-sourcing, complex async, you need DevTools time-travel.

```powershell
npm i @ngrx/store @ngrx/effects @ngrx/store-devtools @ngrx/entity
```

```ts
// actions
export const loadUsers     = createAction('[Users] load');
export const loadUsersOk   = createAction('[Users] load ok',   props<{ users: User[] }>());
export const loadUsersFail = createAction('[Users] load fail', props<{ error: string }>());

// reducer
const initial = { users: [] as User[], loading: false, error: null as string | null };
export const usersReducer = createReducer(initial,
  on(loadUsers,     s => ({ ...s, loading: true })),
  on(loadUsersOk,   (s, { users }) => ({ ...s, users, loading: false })),
  on(loadUsersFail, (s, { error }) => ({ ...s, error, loading: false }))
);

// selectors
export const selectUsersState = createFeatureSelector<typeof initial>('users');
export const selectAllUsers   = createSelector(selectUsersState, s => s.users);

// effect
@Injectable()
export class UsersEffects {
  loadUsers$ = createEffect(() =>
    inject(Actions).pipe(
      ofType(loadUsers),
      switchMap(() => inject(UsersApi).list().pipe(
        map(users => loadUsersOk({ users })),
        catchError(err => of(loadUsersFail({ error: err.message })))
      ))
    )
  );
}

// bootstrap
providers: [
  provideStore({ users: usersReducer }),
  provideEffects([UsersEffects]),
  provideStoreDevtools({ maxAge: 25 })
]

// component
private store = inject(Store);
users = this.store.selectSignal(selectAllUsers);
ngOnInit() { this.store.dispatch(loadUsers()); }
```

`selectSignal` (or `toSignal(store.select(...))`) gives signal interop.

---

## 7. NgRx Entity Adapter

For collections — handles CRUD on normalized state.

```ts
const adapter = createEntityAdapter<User>();
const initial = adapter.getInitialState({ loading: false });

on(loadUsersOk, (s, { users }) => adapter.setAll(users, { ...s, loading: false }))
on(addUser,     (s, { user })  => adapter.addOne(user, s))
on(updateUser,  (s, { user })  => adapter.updateOne({ id: user.id, changes: user }, s))
on(removeUser,  (s, { id })    => adapter.removeOne(id, s))

const { selectAll, selectIds, selectEntities } = adapter.getSelectors();
```

---

## 8. Other Libraries Worth Knowing

| Library | Style | Notes |
|---|---|---|
| **NgRx ComponentStore** | Per-component reactive store | Predecessor to SignalStore |
| **Akita** (Datorama) | Entity-centric, observables | Older but still used |
| **NGXS** | Class-based, decorators, less boilerplate than NgRx | Smaller community |
| **Elf** | Functional, signal-friendly | Modern, modular |
| **Plain RxJS + BehaviorSubject** | DIY | Works, but signals do this better now |

---

## 9. Choosing Where to Put What

```
Local component state    → component signal
Form state               → Reactive Forms (FormGroup)
Server cache             → service + shareReplay (or TanStack Query Angular)
Cross-component shared   → root-provided service with signals
Feature state            → route-scoped service / SignalStore
Global complex           → NgRx Store + Effects + Entity
Router URL               → already in URL — don't duplicate
```

> **URL is state**. Selected tab, filters, pagination — put them in query params, not your store. Browser back button & shareable links come free.

---

## 10. Optimistic UI Pattern

```ts
add(item: CartItem) {
  const previous = this._items();
  this._items.update(arr => [...arr, item]);          // optimistic
  this.http.post('/api/cart', item).pipe(
    catchError(err => {
      this._items.set(previous);                       // rollback
      this.toast.error('Add failed');
      return EMPTY;
    })
  ).subscribe();
}
```

---

## 11. Selector / Computed Best Practices

- Keep computed **pure** — no side effects.
- Compose: `computed(() => derive(a(), b()))`.
- Memoization is built-in — re-evaluates only when reads change.
- Avoid putting expensive transforms inline in templates — wrap in `computed`.

---

## 12. Effects vs Subscriptions

In signal world, `effect()` reacts when signals change:
```ts
effect(() => {
  const id = this.selectedId();
  if (id) this.detail.load(id);
});
```

In NgRx, `Effects` listen to actions and dispatch new ones — for orchestrating API calls, navigation, toasts.

Don't put business logic in components — push it to services / effects / signal stores.

---

## 13. Persisting State

Local persistence (signal sync to `localStorage`):
```ts
constructor() {
  const saved = localStorage.getItem('cart');
  if (saved) this._items.set(JSON.parse(saved));
  effect(() => localStorage.setItem('cart', JSON.stringify(this._items())));
}
```

In SSR, guard with `isPlatformBrowser(PLATFORM_ID)`.

---

## 14. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Mutating arrays/objects in place | Replace with new reference (`[...arr, x]`, `{...obj, k: v}`) |
| Logic scattered between component + service | Push all into service/store |
| Selectors doing expensive work each render | Use `createSelector` (memoized) / `computed` |
| Two sources of truth | URL vs store — pick one for any given piece |
| NgRx for tiny apps | Use a service with signals |
| Forgot to unsubscribe long-lived subscription in a singleton | Singletons live forever — use `takeUntilDestroyed` of consumer |
| Optimistic update with no rollback | Always capture previous, restore on error |
| Effects causing recursion | Effects should be idempotent / guarded |

---

## 15. Senior Interview Q&A

**Q1. When NgRx vs signals?**
Signals for component-local and most feature state. NgRx (or SignalStore) when state is shared, derived, audited (DevTools time-travel), or complex (effects, sagas).

**Q2. What problem does NgRx solve?**
Predictable, single-source-of-truth state with actions → reducers → store; side effects in isolated `Effects`. Enables time-travel debugging and replay.

**Q3. Signals vs Observables — when to use which?**
Signals for **synchronous, derived UI state**. Observables for **async streams** (HTTP, events). Bridge with `toSignal` / `toObservable`.

**Q4. What's `selectSignal`?**
Converts an NgRx selector into a signal — eliminates manual `select(...) | async` plumbing.

**Q5. Should you store form values in NgRx?**
Generally no — Reactive Forms already manage them. Move to store only if the form drives many other components or needs cross-route persistence.

**Q6. How to derive without recomputing?**
`computed(() => ...)` for signals; `createSelector(...)` for NgRx — both memoize.

**Q7. How to do optimistic updates?**
Apply locally first, fire API, rollback on error. Track the previous state.

**Q8. How to debug NgRx?**
StoreDevtools extension — see dispatched actions, diff state, time-travel.

**Q9. Avoiding bloated reducers?**
Split by feature with `provideState`/`createFeature` + entity adapters; keep reducers pure; put side effects in `Effects`.

**Q10. URL as state — example?**
Pagination + filters in `?page=2&q=blue`. Single component reads them via `route.queryParamMap` or input binding. Browser back works, links are shareable.

---

## 16. Cheat Sheet

```ts
// Signal
count = signal(0);
double = computed(() => count() * 2);
effect(() => console.log(count()));

// Service store
private _items = signal<Item[]>([]);
items = this._items.asReadonly();
add(i: Item) { this._items.update(a => [...a, i]); }

// SignalStore
signalStore({ providedIn: 'root' }, withState({...}), withComputed({...}), withMethods(s => ({...})));

// NgRx
store.dispatch(action());
const sig = store.selectSignal(selector);
```

---

## 17. Mental Model

> **State management is a ladder, not a switch. Start with a component signal, climb to a signal-based service, then SignalStore, then full NgRx only when needed. URL is part of your state. Optimistic UI + rollback wins for perceived performance. Pure derivations + immutable updates are the universal pattern at every level.**
