# 08 — HttpClient & RxJS

> **One-liner**: `HttpClient` returns Observables; RxJS gives you the operators to transform, combine, and clean them up. A senior should be fluent in `map / switchMap / catchError / shareReplay / takeUntilDestroyed` and know **interceptors** + **typed requests** cold.

---

## 1. Setup (Standalone, Angular 17+)

```ts
// app.config.ts
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';

providers: [
  provideHttpClient(
    withFetch(),                                // use fetch backend (SSR-friendly, modern)
    withInterceptors([authInterceptor, errorInterceptor])
  )
]
```

`withFetch()` switches the backend from `XMLHttpRequest` to native `fetch` — required for SSR/Angular Universal, supports streaming, better Service Worker integration.

---

## 2. Basic Requests

```ts
@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private base = '/api/users';

  list()                   { return this.http.get<User[]>(this.base); }
  get(id: number)          { return this.http.get<User>(`${this.base}/${id}`); }
  create(body: NewUser)    { return this.http.post<User>(this.base, body); }
  update(id: number, body: Partial<User>) { return this.http.put<User>(`${this.base}/${id}`, body); }
  patch(id: number, body: Partial<User>)  { return this.http.patch<User>(`${this.base}/${id}`, body); }
  remove(id: number)       { return this.http.delete<void>(`${this.base}/${id}`); }
}
```

**Generic** `<User[]>` types the response — without it you get `unknown`.

---

## 3. Headers, Params, Options

```ts
this.http.get<Page<User>>('/api/users', {
  params: new HttpParams()
    .set('page', page)
    .set('size', size)
    .append('tag', 'admin'),
  headers: new HttpHeaders({ 'X-Trace-Id': id }),
  responseType: 'json',
  observe: 'response',          // get full HttpResponse (status, headers, body)
  withCredentials: true
});
```

`HttpParams` and `HttpHeaders` are **immutable** — chain `.set` returns a new instance.

---

## 4. Observing the Full Response

```ts
this.http.get<User>('/api/users/1', { observe: 'response' })
  .pipe(map(r => ({ user: r.body!, total: +r.headers.get('X-Total')! })))
  .subscribe(...);
```

`observe`: `'body'` (default), `'response'`, `'events'` (download/upload progress).

---

## 5. Upload / Download Progress

```ts
this.http.post('/api/files', formData, {
  reportProgress: true,
  observe: 'events'
}).pipe(
  map((e: HttpEvent<unknown>) => {
    switch (e.type) {
      case HttpEventType.UploadProgress: return e.total ? Math.round(100 * e.loaded / e.total) : 0;
      case HttpEventType.Response:       return 100;
      default: return null;
    }
  })
).subscribe(p => this.progress.set(p ?? 0));
```

---

## 6. Functional Interceptors (Angular 15+)

```ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  const cloned = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;
  return next(cloned);
};

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) router.navigateByUrl('/login');
      return throwError(() => err);
    })
  );
};

provideHttpClient(withInterceptors([authInterceptor, errorInterceptor]));
```

Common interceptor patterns: auth tokens, retry, logging, request ID, base URL prefix, caching, error normalization.

---

## 7. Retry & Backoff

```ts
this.http.get('/api/x').pipe(
  retry({
    count: 3,
    delay: (_err, attempt) => timer(2 ** attempt * 200)   // 200ms, 400ms, 800ms
  }),
  catchError(err => of([]))
);
```

Don't retry on **4xx** — only on **5xx** / network errors:
```ts
retry({
  count: 3,
  delay: (err, attempt) => err.status >= 500 ? timer(500 * attempt) : throwError(() => err)
})
```

---

## 8. Caching with `shareReplay`

```ts
private cache$ = this.http.get<Config>('/api/config').pipe(
  shareReplay({ bufferSize: 1, refCount: false })   // shared, replays last
);
config() { return this.cache$; }
```

Or per-key cache:
```ts
private cache = new Map<number, Observable<User>>();

get(id: number) {
  if (!this.cache.has(id)) {
    this.cache.set(id,
      this.http.get<User>(`/api/users/${id}`).pipe(shareReplay(1))
    );
  }
  return this.cache.get(id)!;
}
```

---

## 9. RxJS Operators You'll Use Every Day

| Operator | When |
|---|---|
| `map` | Transform value |
| `tap` | Side effect (logging, set signal) without changing stream |
| `filter` | Drop values |
| `switchMap` | Switch to new Observable, **cancel previous** — typeahead, route changes |
| `concatMap` | Queue — preserve order, no cancel |
| `mergeMap` | Run in parallel |
| `exhaustMap` | Ignore new while one running — save button |
| `combineLatest` | Latest of N streams — derive view models |
| `forkJoin` | All-or-nothing parallel — like `Promise.all` |
| `withLatestFrom` | Combine stream with current value of others |
| `debounceTime` | Pause typing |
| `distinctUntilChanged` | Skip duplicates |
| `startWith` | Initial value |
| `catchError` | Handle errors |
| `retry` | Retry on failure |
| `shareReplay` | Cache last N to multicast |
| `takeUntilDestroyed` | Auto-unsubscribe on component destroy |
| `finalize` | Always runs on completion/error (loading spinner off) |

### Typeahead (canonical example)
```ts
results = toSignal(
  this.search$.pipe(
    debounceTime(300),
    distinctUntilChanged(),
    switchMap(q => q ? this.svc.search(q) : of([]))
  ),
  { initialValue: [] }
);
```

### Save button — ignore double-clicks
```ts
this.save$.pipe(exhaustMap(() => this.svc.save(this.form.value)))
```

### Parallel calls — collect all
```ts
forkJoin({
  user:   this.users.get(id),
  orders: this.orders.byUser(id)
}).subscribe(({ user, orders }) => { ... });
```

---

## 10. Subjects (When You Need Them)

| Subject | Behavior |
|---|---|
| `Subject<T>` | No initial; subscribers get future emissions |
| `BehaviorSubject<T>` | Has current value; new subscribers get it |
| `ReplaySubject<T>(n)` | Replays last `n` to new subscribers |
| `AsyncSubject<T>` | Emits last value only on `complete()` |

> **Senior tip**: In modern Angular, **signals replace most `BehaviorSubject` use cases**. Keep Subjects for true async streams (websockets, multi-emitter events).

```ts
// Modern (signal)
private _items = signal<Item[]>([]);
items = this._items.asReadonly();

// Vs old
private _items$ = new BehaviorSubject<Item[]>([]);
items$ = this._items$.asObservable();
```

---

## 11. Unsubscribing — The Right Way

In modern Angular:
```ts
this.svc.stream()
  .pipe(takeUntilDestroyed())                   // auto-unsubscribe at component destroy
  .subscribe(v => this.value = v);
```

Outside a constructor (need `DestroyRef`):
```ts
private destroyRef = inject(DestroyRef);
this.svc.stream().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(...);
```

Old way (still works):
```ts
private destroyed$ = new Subject<void>();
ngOnDestroy() { this.destroyed$.next(); this.destroyed$.complete(); }
this.svc.stream().pipe(takeUntil(this.destroyed$)).subscribe(...);
```

Better: use `| async` in templates and skip subscriptions entirely.

---

## 12. Error Handling Patterns

```ts
loadUsers() {
  return this.http.get<User[]>('/api/users').pipe(
    catchError((err: HttpErrorResponse) => {
      this.toast.error(`Failed: ${err.status}`);
      return of([]);   // graceful default
    })
  );
}
```

Global error normalization in interceptor:
```ts
export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const msg = err.error?.message ?? err.message ?? 'Network error';
      return throwError(() => ({ status: err.status, message: msg }));
    })
  );
```

---

## 13. Testing HttpClient

```ts
TestBed.configureTestingModule({
  providers: [provideHttpClient(), provideHttpClientTesting(), UserService]
});
const svc  = TestBed.inject(UserService);
const http = TestBed.inject(HttpTestingController);

svc.list().subscribe(users => expect(users.length).toBe(2));

const req = http.expectOne('/api/users');
expect(req.request.method).toBe('GET');
req.flush([{ id: 1 }, { id: 2 }]);

http.verify();              // no pending requests
```

---

## 14. Server-Sent Events / WebSockets

`HttpClient` doesn't speak websockets — use `webSocket` from `rxjs/webSocket`:
```ts
import { webSocket } from 'rxjs/webSocket';
const ws = webSocket<{ type: string }>('wss://api/feed');
ws.pipe(takeUntilDestroyed()).subscribe(msg => ...);
```

---

## 15. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Forgetting `.subscribe()` — request never fires | HTTP observables are cold; subscribe (or use `async` pipe) |
| Multiple subscriptions = multiple network calls | `shareReplay(1)` |
| `switchMap` cancels saves you wanted to keep | Use `concatMap` (sequential) or `mergeMap` (parallel) |
| Retrying on 401 | Filter retry to `>=500` |
| Interceptor recursion (refresh token retries forever) | Add a "retrying" flag in the request to bail out |
| `HttpParams` not mutating | Reassign: `params = params.set('a','b')` |
| Mixed content / CORS | Use proxy in dev (`proxy.conf.json`); proper CORS on server |
| Memory leak with manual subscribe | `takeUntilDestroyed()` / `async` pipe |
| `forkJoin` never completes | Each inner stream must complete (use `take(1)` if needed) |
| `combineLatest` with empty initial | Use `startWith(...)` on each source |

---

## 16. Senior Interview Q&A

**Q1. Why HttpClient over fetch?**
Returns Observables (cancelable, composable with operators), built-in JSON parse, interceptors, progress events, testing helpers, typed responses.

**Q2. Cold vs hot observables?**
Cold creates a new producer per subscriber (HTTP). Hot multicasts a shared producer (WebSocket, Subject). `shareReplay` converts cold to hot with replay.

**Q3. `switchMap` vs `mergeMap` vs `concatMap` vs `exhaustMap`?**
Switch = cancel previous (typeahead). Merge = parallel (independent calls). Concat = queue (preserve order). Exhaust = ignore new while busy (save button).

**Q4. How do interceptors chain?**
They form a pipeline in registration order; each calls `next(req)` to pass the request forward. The response stream comes back through them in reverse.

**Q5. Implement automatic token refresh on 401.**
In response interceptor, catch 401 → call refresh endpoint (sharing the request to avoid stampede) → retry the original request with new token. Mark the refresh request to avoid recursion.

**Q6. How to cancel an HTTP call?**
Unsubscribe (the underlying `XMLHttpRequest` or fetch is aborted). `switchMap` does this automatically.

**Q7. How to deduplicate concurrent calls?**
Cache the observable with `shareReplay(1)` keyed by URL.

**Q8. When use `forkJoin` vs `combineLatest`?**
`forkJoin` = all-or-nothing, completes when **all complete**, emits once. `combineLatest` = continuous, emits whenever **any** emits (after all have emitted once).

**Q9. Why prefer `takeUntilDestroyed()` over `.unsubscribe()` in `ngOnDestroy`?**
Less boilerplate, leverages Angular's `DestroyRef`, works in non-component contexts.

**Q10. Best way to track loading state per request?**
A simple BehaviorSubject/Signal counter incremented on request start, decremented on `finalize`. Or a per-key map for fine-grained spinners.

```ts
finalize(() => this.loading.update(c => c - 1))
```

---

## 17. Cheat Sheet

```ts
// HTTP
this.http.get<T>(url, { params, headers, observe: 'response' });

// Pipeline
input$.pipe(
  debounceTime(300), distinctUntilChanged(),
  switchMap(q => this.svc.search(q)),
  catchError(() => of([])),
  shareReplay(1),
  takeUntilDestroyed()
);

// Parallel
forkJoin({ a: a$, b: b$ }).subscribe(({a, b}) => ...);

// Save (no double-fire)
click$.pipe(exhaustMap(() => save$));

// Cache
const x$ = http.get(...).pipe(shareReplay({ bufferSize: 1, refCount: false }));
```

---

## 18. Mental Model

> **Service exposes Observables → component consumes via `async` pipe or `toSignal`. Use the right `*Map` operator (switch / concat / merge / exhaust) for the situation, cache with `shareReplay`, clean up with `takeUntilDestroyed`. Cross-cutting concerns (auth, errors, retry, logging) go in **functional interceptors**.**
