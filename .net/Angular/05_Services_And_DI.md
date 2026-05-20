# 05 — Services & Dependency Injection

> **One-liner**: Services hold logic + state outside components; Angular's **hierarchical DI** delivers them where needed. Master providers + injection scopes and you've mastered Angular's architecture.

---

## 1. Why Services?

Components should be thin — they handle UI. Services hold:
- HTTP calls
- Business logic
- App-wide state
- Cross-component shared data
- Heavy computations
- Caching

```ts
@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  list() { return this.http.get<User[]>('/api/users'); }
}
```

---

## 2. Creating a Service

```powershell
ng g s services/user --skip-tests=false
```

Generates `user.service.ts`:
```ts
@Injectable({ providedIn: 'root' })
export class UserService {
  constructor() {}
}
```

`providedIn: 'root'` = **app-wide singleton**, tree-shakable (dropped if never used).

---

## 3. Three Ways to Inject

```ts
// (a) Constructor (classic)
constructor(private http: HttpClient, private auth: AuthService) {}

// (b) inject() function (Angular 14+, works outside ctor too)
http = inject(HttpClient);
auth = inject(AuthService);

// (c) Field with @Inject (rare, for tokens)
constructor(@Inject(WINDOW) private win: Window) {}
```

`inject()` is preferred for:
- Standalone functions (guards, resolvers, interceptors)
- Initializer fields
- Cleaner code (no long constructor signatures)

```ts
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() || router.parseUrl('/login');
};
```

---

## 4. Provider Scopes — Hierarchical Injector

Angular has **two injector trees**:
- **Element injector** — per component (via `providers: []` in `@Component`)
- **Module / environment injector** — app-wide (via `providedIn` or `appConfig.providers`)

Lookup goes **child → parent → root**. First match wins.

| Where you provide | Scope | Use for |
|---|---|---|
| `providedIn: 'root'` | Whole app (singleton) | Most services |
| `providedIn: 'platform'` | Across multiple Angular apps on same page | Micro-frontends |
| `providedIn: 'any'` | New instance per lazy-loaded module | Per-feature isolation |
| `appConfig.providers: [...]` | Whole app | Bootstrap-time providers |
| `Route.providers: [...]` | That route subtree | Feature-scoped state |
| `@Component({ providers: [...] })` | This component **+ its children** | Form state, per-card data |

```ts
@Component({ providers: [CartService] })
export class CartPageComponent {}
// every child of CartPageComponent gets the SAME CartService
// sibling components elsewhere get a DIFFERENT instance
```

---

## 5. Provider Recipes

```ts
// Class (default, when you provide a class)
{ provide: Logger, useClass: ConsoleLogger }

// Existing alias
{ provide: NewLogger, useExisting: Logger }

// Value (constants, config)
{ provide: API_URL, useValue: 'https://api.contoso.com' }

// Factory (computed)
{ provide: HttpClient, useFactory: (env: Env) => createHttp(env),
  deps: [ENV_TOKEN] }
```

---

## 6. Injection Tokens

For non-class deps (strings, configs, interfaces):
```ts
export const API_URL = new InjectionToken<string>('API_URL', {
  providedIn: 'root',
  factory: () => environment.apiBase
});

// inject
url = inject(API_URL);
```

Interface tokens (since TS interfaces don't survive compilation):
```ts
export interface AppConfig { apiBase: string; }
export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

// provide
providers: [{ provide: APP_CONFIG, useValue: { apiBase: '...' } }]
```

---

## 7. Modifier Decorators

| Decorator | Effect |
|---|---|
| `@Optional()` | Return `null` instead of throwing if missing |
| `@Self()` | Only look in the component's own injector |
| `@SkipSelf()` | Skip self, start from parent |
| `@Host()` | Stop searching at the component host |

```ts
constructor(@Optional() @SkipSelf() private parent?: TabsetComponent) {
  if (!this.parent) throw new Error('<app-tab> must be inside <app-tabset>');
}
```

Common in directive composition (e.g., a child checks its parent exists).

---

## 8. Service Pattern: State + API

```ts
@Injectable({ providedIn: 'root' })
export class CartStore {
  private http = inject(HttpClient);

  // private writable state
  private _items = signal<CartItem[]>([]);

  // public readonly view
  readonly items = this._items.asReadonly();
  readonly total = computed(() => this._items().reduce((s, i) => s + i.price, 0));

  load() {
    return this.http.get<CartItem[]>('/api/cart').pipe(
      tap(items => this._items.set(items))
    );
  }

  add(item: CartItem) { this._items.update(arr => [...arr, item]); }
  remove(id: string)  { this._items.update(arr => arr.filter(i => i.id !== id)); }
}
```

Senior pattern: **immutable updates**, public readonly signals, methods mutate.

---

## 9. Singleton vs Per-Component Instance

```ts
@Injectable({ providedIn: 'root' })
export class TimerService { /* single shared timer */ }

@Component({ providers: [WizardStateService] })
export class WizardComponent {
  // every <app-wizard> gets its own state
}
```

> Common interview trap: "If `providedIn: 'root'` and a child also lists it in `providers`, what happens?" → child gets its **own** instance; the root singleton still exists for non-overriding consumers.

---

## 10. Multi-Providers

Multiple values registered under the same token (interceptors are the canonical example):

```ts
{ provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor,    multi: true },
{ provide: HTTP_INTERCEPTORS, useClass: LoggingInterceptor, multi: true }
```

Injecting `HTTP_INTERCEPTORS` returns an array.

---

## 11. `APP_INITIALIZER` — Run Before App Starts

```ts
providers: [
  {
    provide: APP_INITIALIZER,
    multi: true,
    useFactory: (cfg: ConfigService) => () => cfg.load(),  // returns Promise/Observable
    deps: [ConfigService]
  }
]
```

Modern equivalent (Angular 19+): `provideAppInitializer(() => inject(ConfigService).load())`.

Use for: loading runtime config, restoring auth token, feature flags.

---

## 12. Lazy-Loaded Feature Scope

Each lazy route gets its own **environment injector**. Services provided in a lazy route's `providers` are singletons **within that feature** only.

```ts
// orders.routes.ts
export const ORDERS_ROUTES: Routes = [
  {
    path: '',
    providers: [OrdersStore, OrdersApi],
    children: [
      { path: '', component: OrdersListComponent },
      { path: ':id', component: OrderDetailComponent }
    ]
  }
];
```

Great for feature isolation + smaller bundles.

---

## 13. Testing Services

```ts
describe('CartStore', () => {
  let store: CartStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), CartStore]
    });
    store = TestBed.inject(CartStore);
    http  = TestBed.inject(HttpTestingController);
  });

  it('loads cart', () => {
    store.load().subscribe();
    const req = http.expectOne('/api/cart');
    req.flush([{ id: '1', price: 10 }]);
    expect(store.items()).toHaveLength(1);
    expect(store.total()).toBe(10);
  });
});
```

Mocking with class swap:
```ts
TestBed.configureTestingModule({
  providers: [{ provide: AuthService, useClass: MockAuthService }]
});
```

---

## 14. Service-Worker / SSR Considerations

- Don't access `window`, `document`, `localStorage` directly in services — inject `DOCUMENT`, `PLATFORM_ID`, or use guards (`isPlatformBrowser`).
```ts
constructor(@Inject(PLATFORM_ID) private platformId: object) {
  if (isPlatformBrowser(this.platformId)) {
    const x = localStorage.getItem('k');
  }
}
```

---

## 15. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Service injected before `APP_INITIALIZER` finishes | Use the initializer to await config |
| Mutating shared state directly | Expose readonly signals + immutable methods |
| Providing the same service in both `root` and a component | Decide one — duplicate instances cause confusing bugs |
| `useFactory` with missing `deps` | Always declare deps array |
| Cyclic DI (`A→B→A`) | Refactor — extract a third service |
| `inject()` outside an injection context | Wrap in `runInInjectionContext` or move into ctor/initializer |
| Memory leak in singleton via subscriptions | Singleton lives forever; clean intervals/sockets on `OnDestroy` of the **consumer** or via service `Destroy` patterns |

---

## 16. Senior Interview Q&A

**Q1. What does `providedIn: 'root'` do?**
Registers a tree-shakable singleton in the **root environment injector**. Used everywhere unless a closer injector overrides.

**Q2. Difference between `useClass`, `useExisting`, `useValue`, `useFactory`?**
- `useClass` — instantiate this class (or replace another).
- `useExisting` — alias an existing provider.
- `useValue` — supply a constant.
- `useFactory` — call a function to compute it (with `deps`).

**Q3. How does Angular's DI find a service?**
Walks up from the component's element injector → parent components → module/environment injector → root. First match wins.

**Q4. Why prefer `inject()` over constructor injection?**
Works in non-class contexts (guards, resolvers, interceptors), enables field initializers, less verbose. Both are equivalent at runtime.

**Q5. What's the difference between `providedIn: 'root'` and providing in `appConfig.providers`?**
Functionally similar today (both root-scoped). `providedIn` is tree-shakable; `appConfig.providers` is always included.

**Q6. How to scope state to a feature?**
Put `providers: [Store]` on the lazy route — each feature gets its own instance.

**Q7. How would you implement feature flags?**
`InjectionToken<FeatureFlags>` loaded by `APP_INITIALIZER`, then injected into a `FeatureFlagService` that exposes signals/observables; use a structural directive `*appFeature="'newCheckout'"`.

**Q8. What is a multi-provider?**
A token that accepts multiple registrations; injection yields an array. `HTTP_INTERCEPTORS` is the canonical example.

**Q9. When would you use `@SkipSelf`?**
A directive that requires a parent of the same type (e.g., `app-tab` needs `app-tabset` ancestor), to avoid resolving itself.

**Q10. Why is cyclic DI a problem and how to fix it?**
Angular cannot resolve which to create first. Fix by extracting shared logic into a third service, using events/streams instead of direct injection, or delaying access via `inject(forwardRef(() => X))`.

---

## 17. Cheat Sheet

```ts
// service
@Injectable({ providedIn: 'root' })
export class FooService { http = inject(HttpClient); }

// token
export const API = new InjectionToken<string>('API', {
  providedIn: 'root', factory: () => environment.apiBase
});

// interceptor multi-provider (legacy)
{ provide: HTTP_INTERCEPTORS, useClass: Auth, multi: true }

// app initializer
provideAppInitializer(() => inject(ConfigService).load())

// feature-scoped
{ path: 'orders', providers: [OrdersStore], loadChildren: () => ... }
```

---

## 18. Mental Model

> **DI in Angular = hierarchical injectors + provider recipes. Use `providedIn: 'root'` for singletons, component `providers` for per-instance, route `providers` for feature scope. Services hold state via signals, expose readonly views, mutate via methods. Master tokens, multi-providers, and `APP_INITIALIZER` and you can architect any Angular app.**
